using System.Collections.Generic;
using UnityEngine;
using LifeSimulation.Simulation.World;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// The planet, drawn at the resolution the camera is actually close enough to see.
    ///
    /// <para><b>What this replaces.</b> One icosphere at subdivision 5: about 20,000 triangles of
    /// roughly 19 metres each, fixed, over the whole 500-unit sphere. Flying down to the surface
    /// anywhere outside the arena meant landing on a facet the size of a house, and zooming added
    /// nothing, because there was nothing more to add. Twenty base faces are now a quadtree each,
    /// split where the camera is and left coarse everywhere else.</para>
    ///
    /// <para><b>Detail is real, not just denser.</b> Each chunk band-limits its elevation to its own
    /// grid, so a split chunk gains octaves rather than redrawing a smooth surface with more
    /// triangles. Sampling past that limit is what turned the globe into static the first time, so
    /// the limit is derived per chunk rather than chosen.</para>
    ///
    /// <para><b>Presentation only.</b> No simulation state is read or written, no hash moves. The
    /// arena and its creatures are drawn by a separate, finer patch and are unaffected.</para>
    /// </summary>
    public sealed class PlanetChunkedSurface : MonoBehaviour
    {
        /// <summary>
        /// Chunk meshes built per frame.
        ///
        /// <para>A chunk is 153 elevation samples and its mesh, which is cheap once and ruinous four
        /// hundred times in the frame a camera crosses a threshold. Over budget, the parent keeps
        /// being drawn until its children are ready - coarse for a few frames, which is a great deal
        /// better than a stall, and invisible at flying speed.</para>
        /// </summary>
        private const int BuildsPerFrame = 6;

        private sealed class Node
        {
            public Vector3 CornerA;
            public Vector3 CornerB;
            public Vector3 CornerC;
            public Vector3 Centre;
            public int Depth;
            public Node[] Children;
            public GameObject View;
            public bool Built;
        }

        private readonly List<Node> _roots = new List<Node>();

        private Camera _camera;
        private Material _material;
        private PlateStructure _plates;
        private TerrainSettings _settings;
        private int _seed;
        private float _drawRadius;
        private float _reliefFraction;
        private Vector3 _arenaDirection = Vector3.up;
        private int _budget;

        /// <summary>
        /// Point this at a world. Safe to call again when the seed or the terrain settings change -
        /// the tree is thrown away and rebuilt lazily rather than patched.
        /// </summary>
        public void Configure(
            Camera camera, Material material, int seed, PlateStructure plates, TerrainSettings settings,
            float drawRadius, float reliefFraction, Vector3 arenaDirection)
        {
            _camera = camera;
            _material = material;
            _seed = seed;
            _plates = plates;
            _settings = settings;
            _drawRadius = drawRadius;
            _reliefFraction = reliefFraction;
            _arenaDirection = arenaDirection.normalized;

            Clear();
            BuildRoots();
        }

        private void Clear()
        {
            for (int index = 0; index < _roots.Count; index++) Release(_roots[index]);
            _roots.Clear();
        }

        /// <summary>
        /// Throw a chunk and everything under it away.
        ///
        /// <para>The mesh goes too. A discarded <c>Mesh</c> is not collected along with the GameObject
        /// that referenced it, and a camera crossing thresholds discards them by the hundred.</para>
        /// </summary>
        private void Release(Node node)
        {
            if (node.View != null)
            {
                var filter = node.View.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null) Discard(filter.sharedMesh);
                Discard(node.View);
                node.View = null;
            }

            node.Built = false;
            if (node.Children == null) return;

            for (int index = 0; index < node.Children.Length; index++) Release(node.Children[index]);
            node.Children = null;
        }

        /// <summary>
        /// Destroy something, from either side of Play mode.
        ///
        /// <para><c>Object.Destroy</c> defers to the end of the frame, and outside Play mode there is
        /// no frame - so in the offline capture the discarded chunks would still be in the scene when
        /// the picture was taken.</para>
        /// </summary>
        private static void Discard(Object doomed)
        {
            if (Application.isPlaying) Object.Destroy(doomed);
            else Object.DestroyImmediate(doomed);
        }

        /// <summary>The twenty faces of the bare icosahedron, each the root of its own quadtree.</summary>
        private void BuildRoots()
        {
            IcoSphere.Build(0, out Vector3[] directions, out int[] triangles);
            for (int index = 0; index < triangles.Length; index += 3)
            {
                _roots.Add(Make(
                    directions[triangles[index]],
                    directions[triangles[index + 1]],
                    directions[triangles[index + 2]],
                    0));
            }
        }

        private static Node Make(Vector3 cornerA, Vector3 cornerB, Vector3 cornerC, int depth)
        {
            return new Node
            {
                CornerA = cornerA,
                CornerB = cornerB,
                CornerC = cornerC,
                Centre = (cornerA + cornerB + cornerC).normalized,
                Depth = depth,
            };
        }

        private void LateUpdate()
        {
            if (_camera == null) return;

            Refresh(_camera.transform.position, passes: 1);
        }

        /// <summary>
        /// Bring the tree up to date for a viewpoint, running the build queue this many times.
        ///
        /// <para>Play mode calls this once per frame from the camera. <b>The offline capture calls it
        /// with enough passes to drain the queue</b>, which is how a PNG can show the same surface a
        /// person flying there would see: the capture renders a settled tree rather than the first
        /// six chunks of one. Without a way in here that does not involve a running frame loop, the
        /// capture would have had to build the planet its own way - and a capture that cannot
        /// reproduce the runtime is a second implementation, not an instrument.</para>
        /// </summary>
        public void Refresh(Vector3 viewpoint, int passes)
        {
            if (_roots.Count == 0) return;

            Vector3 local = transform.InverseTransformPoint(viewpoint);
            for (int pass = 0; pass < passes; pass++)
            {
                _budget = BuildsPerFrame;
                bool settled = true;
                for (int index = 0; index < _roots.Count; index++)
                {
                    settled &= Select(_roots[index], local);
                }

                if (settled && _budget == BuildsPerFrame) return;
            }
        }

        /// <summary>
        /// Decide what this chunk contributes, and recurse.
        ///
        /// <para>Returns whether the subtree is drawable right now. A parent whose children are still
        /// queued keeps drawing itself, which is what makes the build budget safe: the planet is
        /// never missing a piece, only ever coarser than it will be a few frames later.</para>
        /// </summary>
        private bool Select(Node node, Vector3 cameraLocal)
        {
            double edge = PlanetChunkLod.EdgeAt(_drawRadius, node.Depth);
            double angularRadius = edge / _drawRadius * 0.6d;

            if (PlanetChunkLod.HiddenByArena(Vector3.Angle(node.Centre, _arenaDirection) * Mathf.Deg2Rad, angularRadius))
            {
                Hide(node);
                return true;
            }

            double distance = Distance(node, cameraLocal, edge);
            bool split = node.Children != null
                ? !PlanetChunkLod.ShouldMerge(edge, distance, node.Depth + 1)
                : PlanetChunkLod.ShouldSplit(edge, distance, node.Depth, PlanetChunkLod.MaximumDepth);

            if (!split)
            {
                if (node.Children != null) DropChildren(node);
                return Draw(node);
            }

            EnsureChildren(node);

            bool ready = true;
            for (int index = 0; index < node.Children.Length; index++)
            {
                ready &= Select(node.Children[index], cameraLocal);
            }

            // Only stop drawing this chunk once every piece replacing it exists, or the planet has a
            // hole in it for as long as the queue takes to drain.
            if (!ready) return Draw(node);

            SetVisible(node, false);
            return true;
        }

        /// <summary>
        /// What the tree currently looks like: how many chunks are drawn at each depth.
        ///
        /// <para>The only way to tell a level-of-detail artefact from a terrain one without guessing
        /// at pixels. A seam in a render is either two depths meeting or it is the ground; this says
        /// which depths are on screen at all.</para>
        /// </summary>
        public string Describe()
        {
            var counts = new int[PlanetChunkLod.MaximumDepth + 1];
            for (int index = 0; index < _roots.Count; index++) Count(_roots[index], counts);

            var text = new System.Text.StringBuilder("chunks drawn:");
            for (int depth = 0; depth < counts.Length; depth++)
            {
                if (counts[depth] > 0) text.Append(" depth ").Append(depth).Append("=").Append(counts[depth]);
            }

            return text.ToString();
        }

        private static void Count(Node node, int[] counts)
        {
            if (node.View != null && node.View.activeSelf) counts[node.Depth]++;
            if (node.Children == null) return;

            for (int index = 0; index < node.Children.Length; index++) Count(node.Children[index], counts);
        }

        /// <summary>Camera distance to the chunk, measured to its nearest part rather than its middle.</summary>
        private double Distance(Node node, Vector3 cameraLocal, double edge)
        {
            float centre = Vector3.Distance(cameraLocal, node.Centre * _drawRadius);
            double nearest = centre - (edge * 0.5d);
            return nearest < 0d ? 0d : nearest;
        }

        private void EnsureChildren(Node node)
        {
            if (node.Children != null) return;

            Vector3 ab = ((node.CornerA + node.CornerB) * 0.5f).normalized;
            Vector3 bc = ((node.CornerB + node.CornerC) * 0.5f).normalized;
            Vector3 ca = ((node.CornerC + node.CornerA) * 0.5f).normalized;

            node.Children = new[]
            {
                Make(node.CornerA, ab, ca, node.Depth + 1),
                Make(ab, node.CornerB, bc, node.Depth + 1),
                Make(ca, bc, node.CornerC, node.Depth + 1),
                Make(ab, bc, ca, node.Depth + 1),
            };
        }

        private void DropChildren(Node node)
        {
            if (node.Children == null) return;

            for (int index = 0; index < node.Children.Length; index++) Release(node.Children[index]);
            node.Children = null;
        }

        /// <summary>Show this chunk, building it if there is budget left. False if it is not ready.</summary>
        private bool Draw(Node node)
        {
            if (!node.Built)
            {
                if (_budget <= 0) return false;

                _budget--;
                BuildMesh(node);
            }

            SetVisible(node, true);
            return true;
        }

        private void Hide(Node node)
        {
            SetVisible(node, false);
            DropChildren(node);
        }

        private void SetVisible(Node node, bool visible)
        {
            if (node.View != null && node.View.activeSelf != visible) node.View.SetActive(visible);
        }

        private void BuildMesh(Node node)
        {
            PlanetChunkMesh.Build(
                _seed, _plates, _settings,
                node.CornerA, node.CornerB, node.CornerC, node.Depth,
                _drawRadius, _reliefFraction,
                out Vector3[] vertices, out Color[] colors, out int[] triangles);

            if (node.View == null)
            {
                node.View = new GameObject("Chunk " + node.Depth);
                node.View.transform.SetParent(transform, false);
                node.View.AddComponent<MeshRenderer>().sharedMaterial = _material;
                node.View.AddComponent<MeshFilter>();
            }

            node.View.GetComponent<MeshFilter>().sharedMesh =
                TerrainMeshBuilder.FlatShaded(vertices, colors, triangles, "Planet Chunk");
            node.Built = true;
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
