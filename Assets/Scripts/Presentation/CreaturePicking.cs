using System.Collections.Generic;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// One creature as the picker sees it: where it landed on screen, and how far in front of the
    /// camera it is. Deliberately plain floats rather than Unity types so the choice can be tested
    /// headlessly - the projection that produces these stays in the presenter.
    /// </summary>
    public readonly struct CreaturePickCandidate
    {
        public CreaturePickCandidate(CreatureId id, float screenX, float screenY, float depth)
        {
            Id = id;
            ScreenX = screenX;
            ScreenY = screenY;
            Depth = depth;
        }

        public CreatureId Id { get; }

        public float ScreenX { get; }

        public float ScreenY { get; }

        /// <summary>Distance in front of the camera. Zero or negative means behind it.</summary>
        public float Depth { get; }
    }

    /// <summary>
    /// Chooses which creature a click selects.
    ///
    /// <para><b>Why this is not a raycast.</b> Selection used <c>Physics.Raycast</c> and compared the
    /// hit transform against the creature's view. That only ever worked because
    /// <c>GameObject.CreatePrimitive</c> returns a capsule with a collider on that same transform.
    /// Once creatures became instantiated FBX models it broke silently: nothing in the project adds a
    /// collider to a model, so the ray hit nothing and clicking a creature did nothing at all.</para>
    ///
    /// <para>Adding colliders back would mean a collider per creature moved every frame, which forces
    /// the physics broadphase to rebuild for a static collider, on 126 creatures, to answer a question
    /// that is one screen-space comparison. Terrain carries no collider either, so there is no
    /// occlusion to respect. Projecting each creature and taking the closest is cheaper, simpler, and
    /// more forgiving to click - which matters because these animals are small on screen and, as the
    /// user reports, frequently overlapping.</para>
    /// </summary>
    public static class CreaturePicking
    {
        /// <summary>
        /// The creature nearest the click within <paramref name="maxRadiusPixels"/>, or false if none
        /// is close enough.
        ///
        /// <para>Screen distance decides, so clicking an animal cannot select its neighbour. Depth
        /// only breaks ties, which is the overlapping-creature case: of two animals at the same point
        /// the nearer one is the one being pointed at.</para>
        /// </summary>
        public static bool TrySelectClosest(
            float clickX,
            float clickY,
            IReadOnlyList<CreaturePickCandidate> candidates,
            float maxRadiusPixels,
            out CreatureId selected)
        {
            selected = default;
            if (candidates == null || candidates.Count == 0 || maxRadiusPixels <= 0f)
            {
                return false;
            }

            float maximumSquared = maxRadiusPixels * maxRadiusPixels;
            float bestSquared = float.MaxValue;
            float bestDepth = float.MaxValue;
            bool found = false;

            for (int index = 0; index < candidates.Count; index++)
            {
                CreaturePickCandidate candidate = candidates[index];
                if (candidate.Depth <= 0f)
                {
                    continue;
                }

                float offsetX = candidate.ScreenX - clickX;
                float offsetY = candidate.ScreenY - clickY;
                float distanceSquared = (offsetX * offsetX) + (offsetY * offsetY);
                if (distanceSquared > maximumSquared)
                {
                    continue;
                }

                // Screen distance first, depth only as the tie-break. Comparing squared distances
                // exactly is right here rather than sloppy: two creatures drawn at the same point
                // produce identical values, which is exactly the case depth exists to settle.
                if (distanceSquared < bestSquared
                    || (distanceSquared == bestSquared && candidate.Depth < bestDepth))
                {
                    bestSquared = distanceSquared;
                    bestDepth = candidate.Depth;
                    selected = candidate.Id;
                    found = true;
                }
            }

            return found;
        }
    }
}
