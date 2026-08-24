using UnityEngine;
using LifeSimulation.Simulation.World;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// One patch of the planet's surface, built from the three corners of a spherical triangle.
    ///
    /// <para>Every chunk in the tree is one of these. The corners come from splitting an icosahedron
    /// face in four, over and over, so the chunks tile the sphere exactly and nothing degenerates at
    /// a pole - the same reason the single-mesh planet was an icosphere and not a lat/long grid.</para>
    ///
    /// <para>The elevation each chunk carries is band-limited to its own grid, not the planet's, so
    /// splitting a chunk genuinely adds octaves rather than drawing the same smooth surface with more
    /// triangles. That is the entire point of the exercise, and it is one argument:
    /// <see cref="PlanetChunkLod.DetailLevelFor"/>.</para>
    /// </summary>
    public static class PlanetChunkMesh
    {
        /// <summary>
        /// Build one chunk, skirt included, in planet-local space.
        /// </summary>
        /// <param name="cornerA">Unit-sphere directions of the chunk's three corners.</param>
        /// <param name="depth">Split depth, which fixes the sampling band limit.</param>
        /// <param name="drawRadius">Radius the planet is drawn at.</param>
        /// <param name="reliefFraction">Height per unit of elevation, as a fraction of the radius.</param>
        public static void Build(
            int seed, PlateStructure plates, TerrainSettings settings,
            Vector3 cornerA, Vector3 cornerB, Vector3 cornerC, int depth,
            float drawRadius, float reliefFraction,
            out Vector3[] vertices, out Color[] colors, out int[] triangles)
        {
            int segments = PlanetChunkLod.Segments;
            double maximumFrequency = PlanetTerrain.MaximumFrequencyFor(
                IcoSphere.SamplesAroundEquator(PlanetChunkLod.DetailLevelFor(depth)));

            int surfaceCount = (segments + 1) * (segments + 2) / 2;
            var surface = new Vector3[surfaceCount];
            var shades = new Color[surfaceCount];

            for (int row = 0; row <= segments; row++)
            {
                Vector3 left = Vector3.Slerp(cornerA, cornerB, row / (float)segments);
                Vector3 right = Vector3.Slerp(cornerA, cornerC, row / (float)segments);
                for (int column = 0; column <= row; column++)
                {
                    Vector3 direction = row == 0
                        ? cornerA
                        : Vector3.Slerp(left, right, column / (float)row);

                    PlanetSample sample = PlanetTerrain.Sample(
                        seed, plates, direction.x, direction.y, direction.z, maximumFrequency, settings);

                    int index = Index(row, column);
                    surface[index] = direction * (drawRadius * (1f + (sample.Elevation * reliefFraction)));
                    shades[index] = PlanetBiome.Shade(sample);
                }
            }

            BuildSurfaceTriangles(segments, surface, out int[] surfaceTriangles);
            AppendSkirt(
                segments, surface, shades, surfaceTriangles,
                (float)PlanetChunkLod.SkirtDepth(EdgeLength(cornerA, cornerB, drawRadius)),
                out vertices, out colors, out triangles);
        }

        /// <summary>Position in the row-major triangular grid: row r holds r + 1 columns.</summary>
        private static int Index(int row, int column)
        {
            return (row * (row + 1) / 2) + column;
        }

        private static float EdgeLength(Vector3 cornerA, Vector3 cornerB, float drawRadius)
        {
            return Vector3.Angle(cornerA, cornerB) * Mathf.Deg2Rad * drawRadius;
        }

        /// <summary>
        /// The chunk's own triangles, wound so they face outward.
        ///
        /// <para>The winding is checked against the surface rather than assumed, because the four
        /// children of a face do not all inherit their parent's orientation and a chunk wound the
        /// wrong way is invisible from outside and solid from within - a hole in the planet that only
        /// appears at certain angles.</para>
        /// </summary>
        private static void BuildSurfaceTriangles(int segments, Vector3[] surface, out int[] triangles)
        {
            triangles = new int[segments * segments * 3];
            int next = 0;
            for (int row = 1; row <= segments; row++)
            {
                for (int column = 0; column < row; column++)
                {
                    triangles[next++] = Index(row - 1, column);
                    triangles[next++] = Index(row, column);
                    triangles[next++] = Index(row, column + 1);

                    if (column < row - 1)
                    {
                        triangles[next++] = Index(row - 1, column);
                        triangles[next++] = Index(row, column + 1);
                        triangles[next++] = Index(row - 1, column + 1);
                    }
                }
            }

            Vector3 a = surface[triangles[0]];
            Vector3 b = surface[triangles[1]];
            Vector3 c = surface[triangles[2]];
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), a) < 0f)
            {
                for (int index = 0; index < triangles.Length; index += 3)
                {
                    int swap = triangles[index + 1];
                    triangles[index + 1] = triangles[index + 2];
                    triangles[index + 2] = swap;
                }
            }
        }

        /// <summary>
        /// Hang a rim inward from all three edges.
        ///
        /// <para>A neighbour one level coarser samples the shared edge at half the resolution, so the
        /// two edges do not meet and the gap between them is a crack showing the sky through the
        /// planet. The rim fills it. It is geometry rather than a shader trick because it has to work
        /// in the offline capture too, which shares this code path on purpose.</para>
        /// </summary>
        private static void AppendSkirt(
            int segments, Vector3[] surface, Color[] shades, int[] surfaceTriangles, float skirtDepth,
            out Vector3[] vertices, out Color[] colors, out int[] triangles)
        {
            int[] border = BorderRing(segments);

            vertices = new Vector3[surface.Length + border.Length];
            colors = new Color[vertices.Length];
            surface.CopyTo(vertices, 0);
            shades.CopyTo(colors, 0);

            for (int step = 0; step < border.Length; step++)
            {
                Vector3 point = surface[border[step]];
                float length = point.magnitude;
                float pulled = Mathf.Max(length - skirtDepth, length * 0.5f);
                vertices[surface.Length + step] = point * (pulled / length);
                colors[surface.Length + step] = shades[border[step]];
            }

            // Both windings. Which way round the ring runs depends on the chunk's own orientation,
            // and a one-sided skirt facing inward is invisible from outside - which is to say, it is
            // not there at all, and the crack it was added to fill is still open. A skirt is only
            // ever seen edge-on, so the second copy costs a few hundred indices and nothing else.
            triangles = new int[surfaceTriangles.Length + (border.Length * 12)];
            surfaceTriangles.CopyTo(triangles, 0);

            int next = surfaceTriangles.Length;
            for (int step = 0; step < border.Length; step++)
            {
                int here = border[step];
                int there = border[(step + 1) % border.Length];
                int underHere = surface.Length + step;
                int underThere = surface.Length + ((step + 1) % border.Length);

                triangles[next++] = here;
                triangles[next++] = underHere;
                triangles[next++] = there;
                triangles[next++] = there;
                triangles[next++] = underHere;
                triangles[next++] = underThere;

                triangles[next++] = here;
                triangles[next++] = there;
                triangles[next++] = underHere;
                triangles[next++] = there;
                triangles[next++] = underThere;
                triangles[next++] = underHere;
            }
        }

        /// <summary>The chunk's outline, once round, in order: down one edge, along, and back up.</summary>
        private static int[] BorderRing(int segments)
        {
            var ring = new int[segments * 3];
            int next = 0;

            // A to B, down the left edge.
            for (int row = 0; row < segments; row++) ring[next++] = Index(row, 0);

            // B to C, along the bottom.
            for (int column = 0; column < segments; column++) ring[next++] = Index(segments, column);

            // C back to A, up the right edge.
            for (int row = segments; row > 0; row--) ring[next++] = Index(row, row);

            return ring;
        }
    }
}
