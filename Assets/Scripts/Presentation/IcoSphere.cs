using System;
using System.Collections.Generic;
using UnityEngine;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// A subdivided icosahedron: a sphere with no poles.
    ///
    /// <para><b>Why this replaces the lat/lon sphere.</b> A UV sphere converges every longitude
    /// vertex onto a single point at each pole, so triangles there degenerate into slivers and their
    /// normals fan out - which renders as a starburst pinch, the "bottom of a balloon" artefact. It
    /// is not a tuning problem: the geometry is singular at the poles by construction. An icosahedron
    /// subdivided on the sphere has no singular point, and its triangles are within a few percent of
    /// the same size everywhere, so detail is distributed evenly instead of bunching at the poles and
    /// stretching at the equator.</para>
    ///
    /// <para>It also happens to be the shape that gives the low-poly look: near-equilateral triangles
    /// of uniform size, which is what flat shading needs to read as facets rather than as noise.</para>
    /// </summary>
    public static class IcoSphere
    {
        /// <summary>
        /// Unit-sphere vertex directions and triangle indices at a subdivision level.
        /// Level 0 is the bare icosahedron (20 faces); each level multiplies faces by four.
        /// </summary>
        public static void Build(int subdivisions, out Vector3[] directions, out int[] triangles)
        {
            if (subdivisions < 0) throw new ArgumentOutOfRangeException(nameof(subdivisions));

            // Golden-ratio rectangles give the 12 icosahedron vertices.
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var vertices = new List<Vector3>
            {
                new Vector3(-1f, t, 0f).normalized, new Vector3(1f, t, 0f).normalized,
                new Vector3(-1f, -t, 0f).normalized, new Vector3(1f, -t, 0f).normalized,
                new Vector3(0f, -1f, t).normalized, new Vector3(0f, 1f, t).normalized,
                new Vector3(0f, -1f, -t).normalized, new Vector3(0f, 1f, -t).normalized,
                new Vector3(t, 0f, -1f).normalized, new Vector3(t, 0f, 1f).normalized,
                new Vector3(-t, 0f, -1f).normalized, new Vector3(-t, 0f, 1f).normalized,
            };

            var faces = new List<int>
            {
                0, 11, 5,  0, 5, 1,   0, 1, 7,   0, 7, 10,  0, 10, 11,
                1, 5, 9,   5, 11, 4,  11, 10, 2, 10, 7, 6,  7, 1, 8,
                3, 9, 4,   3, 4, 2,   3, 2, 6,   3, 6, 8,   3, 8, 9,
                4, 9, 5,   2, 4, 11,  6, 2, 10,  8, 6, 7,   9, 8, 1,
            };

            for (int level = 0; level < subdivisions; level++)
            {
                var midpoints = new Dictionary<long, int>();
                var next = new List<int>(faces.Count * 4);
                for (int index = 0; index < faces.Count; index += 3)
                {
                    int a = faces[index];
                    int b = faces[index + 1];
                    int c = faces[index + 2];
                    int ab = Midpoint(a, b, vertices, midpoints);
                    int bc = Midpoint(b, c, vertices, midpoints);
                    int ca = Midpoint(c, a, vertices, midpoints);

                    next.Add(a); next.Add(ab); next.Add(ca);
                    next.Add(b); next.Add(bc); next.Add(ab);
                    next.Add(c); next.Add(ca); next.Add(bc);
                    next.Add(ab); next.Add(bc); next.Add(ca);
                }

                faces = next;
            }

            directions = vertices.ToArray();
            triangles = faces.ToArray();
        }

        /// <summary>Faces at a subdivision level, for choosing one against a sampling budget.</summary>
        public static int FaceCount(int subdivisions)
        {
            return 20 * (int)Math.Pow(4d, subdivisions);
        }

        /// <summary>
        /// Roughly how many triangles span a great circle, which is the icosphere's equivalent of
        /// "samples around the equator" for choosing a Nyquist limit.
        /// </summary>
        public static int SamplesAroundEquator(int subdivisions)
        {
            // Surface area 4*pi split into FaceCount triangles; the edge length follows, and a great
            // circle of length 2*pi holds that many.
            double faceArea = 4d * Math.PI / FaceCount(subdivisions);
            double edge = Math.Sqrt(faceArea * 4d / Math.Sqrt(3d));
            return Math.Max(8, (int)(2d * Math.PI / edge));
        }

        private static int Midpoint(int a, int b, List<Vector3> vertices, Dictionary<long, int> cache)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (cache.TryGetValue(key, out int existing)) return existing;

            Vector3 middle = ((vertices[a] + vertices[b]) * 0.5f).normalized;
            vertices.Add(middle);
            int index = vertices.Count - 1;
            cache[key] = index;
            return index;
        }
    }
}
