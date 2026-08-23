using LifeSimulation.Simulation.Environment;
using UnityEngine;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// Where a simulation position is drawn.
    ///
    /// <para><b>The simulation is flat and stays flat.</b> Creature positions are two floats on a
    /// 50-unit square with Euclidean distances, and nothing here changes that - no hash moves, no
    /// recorded result is affected. This is a display transform applied after the tick, which is why
    /// a round world costs a file rather than a rewrite of the spatial model.</para>
    ///
    /// <para><b>The planet's centre sits at (0, -R, 0)</b>, so the arena's centre lands exactly on
    /// the origin with its surface normal pointing straight up. At the centre the mapping is the
    /// identity, which means the existing camera rig - focus at the origin, up is +Y, pitch and pan
    /// unchanged - keeps working without knowing any of this happened. Zooming out simply reveals the
    /// globe the patch has been sitting on all along.</para>
    ///
    /// <para><b>Scale is true, and it costs nothing.</b> The globe preview draws at radius 60 with a
    /// relief fraction of 0.06, which is 3.6 units of height per elevation unit; scaled to the real
    /// 500-unit radius that is <c>3.6 * 500 / 60 = 30</c> - exactly the arena's own 30 units per
    /// elevation unit. The two views were already the same shape at different sizes, so drawing one
    /// continuous world at true scale changes the character of the terrain not at all.</para>
    ///
    /// <para><b>What this does not fix.</b> Only the patch is inhabited; the rest of the planet is
    /// scenery, and stays that way until the spatial model itself is spherical. The simulation also
    /// still believes the patch is flat while the screen shows it curved - across 50 units on a
    /// 500-unit sphere that disagreement is a sagitta of <c>R(1 - cos(w/2R)) = 0.63</c> units, under
    /// one creature, invisible, and real.</para>
    /// </summary>
    public static class ArenaProjection
    {
        /// <summary>True planet radius, matching <see cref="EnvironmentField.SphereRadius"/>.</summary>
        public const float PlanetRadius = (float)EnvironmentField.SphereRadius;

        /// <summary>
        /// Lifts the arena patch clear of the backdrop globe drawn from the same field. Both meshes
        /// evaluate the same elevation, so without it they are coplanar and z-fight; at 500 units of
        /// radius this offset is four thousandths of a percent and cannot be seen.
        /// </summary>
        public const float PatchLift = 0.02f;

        /// <summary>Whether positions curve onto the planet, or stay on the flat plane.</summary>
        public static bool Spherical { get; set; }

        /// <summary>Planet centre in world space: directly below the arena.</summary>
        public static Vector3 Centre
        {
            get { return new Vector3(0f, -PlanetRadius, 0f); }
        }

        /// <summary>
        /// Surface normal above an arena position - which is also the direction from the planet's
        /// centre. Creatures are oriented along this so they stand on the ground rather than leaning
        /// with it.
        /// </summary>
        public static Vector3 Normal(float x, float z)
        {
            if (!Spherical) return Vector3.up;

            // Moving north (+z) tips the normal toward +z; moving east (+x) tips it toward +x. Small
            // angles either way - the whole arena spans 0.1 radian - but done exactly rather than
            // linearised, because the same function has to hold if the window ever grows.
            float northRadians = z / PlanetRadius;
            float eastRadians = x / PlanetRadius;
            Quaternion north = Quaternion.AngleAxis(northRadians * Mathf.Rad2Deg, Vector3.right);
            Quaternion east = Quaternion.AngleAxis(-eastRadians * Mathf.Rad2Deg, Vector3.forward);
            return east * north * Vector3.up;
        }

        /// <summary>Where an arena position at a given height above the surface is drawn.</summary>
        public static Vector3 ToWorld(float x, float z, float height)
        {
            if (!Spherical) return new Vector3(x, height, z);
            return Centre + (Normal(x, z) * (PlanetRadius + height));
        }

        /// <summary>Rotation that stands an upright object on the surface at this position.</summary>
        public static Quaternion Upright(float x, float z)
        {
            return Spherical ? Quaternion.FromToRotation(Vector3.up, Normal(x, z)) : Quaternion.identity;
        }

        /// <summary>
        /// Remap a mesh built on the flat plane onto the sphere, in place.
        ///
        /// <para>Deliberately a post-pass over <see cref="TerrainMeshBuilder.BuildPatch"/>'s output
        /// rather than a second builder. A spherical copy of the patch builder would be a second
        /// implementation of the same geometry, and the last time this project had two of those they
        /// drifted until the diagnostics described a mesh nobody was looking at.</para>
        /// </summary>
        public static void ProjectVertices(Vector3[] vertices)
        {
            if (!Spherical || vertices == null) return;
            for (int index = 0; index < vertices.Length; index++)
            {
                Vector3 flat = vertices[index];
                vertices[index] = ToWorld(flat.x, flat.z, flat.y + PatchLift);
            }
        }
    }
}
