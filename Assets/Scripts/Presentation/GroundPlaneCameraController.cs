using UnityEngine;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// Orbit camera for the arena, and for the planet the arena sits on.
    ///
    /// <para>Left drag orbits - yaw and pitch. Right drag pans. Wheel zooms, multiplicatively, so a
    /// notch covers the same fraction of the distance whether you are on top of a creature or looking
    /// at a continent.</para>
    /// </summary>
    public sealed class GroundPlaneCameraController : MonoBehaviour
    {
        private const float MinimumDistance = 8f;
        private const float DefaultMaximumDistance = 60f;
        private const float DefaultPanLimit = 35f;

        /// <summary>
        /// Fraction of the current distance one wheel notch covers. Zoom is multiplicative, so the
        /// notches between two distances are <c>ln(far/near) / ln(1 + step)</c> - at 0.12 that is
        /// thirty-four notches from 32 units out to a 500-unit planet, which is not a zoom control,
        /// it is a chore. Callers showing something that large pass a coarser step.
        /// </summary>
        private const float DefaultZoomStep = 0.12f;

        /// <summary>
        /// Pitch limits for the arena: never below the ground, never quite overhead. Looking up from
        /// underneath a heightfield shows its unlit back faces, which is why the floor exists.
        /// </summary>
        private const float DefaultMinimumPitch = 25f;
        private const float DefaultMaximumPitch = 75f;

        private Vector3 _focus;
        private float _distance = 32f;
        private float _pitch = 52f;
        private float _yaw;
        private float _maximumDistance = DefaultMaximumDistance;
        private float _panLimit = DefaultPanLimit;
        private float _zoomStep = DefaultZoomStep;
        private float _minimumPitch = DefaultMinimumPitch;
        private float _maximumPitch = DefaultMaximumPitch;

        private bool _hasDistantFocus;
        private Vector3 _distantFocus;

        /// <summary>
        /// Point the camera at something of a given size and let it pull back far enough to see it.
        ///
        /// <para>The zoom ceiling and pan clamp are sized for the 50-unit arena, so a 400-unit
        /// terrain preview could not be framed at all - it filled the view and read as a flat plane
        /// because only its middle was ever visible. Callers showing something larger raise the
        /// limits for as long as it is on screen.</para>
        /// </summary>
        public void Frame(float radius)
        {
            _focus = Vector3.zero;
            _hasDistantFocus = false;
            _maximumDistance = Mathf.Max(DefaultMaximumDistance, radius * 2.6f);
            _panLimit = Mathf.Max(DefaultPanLimit, radius);
            _distance = Mathf.Clamp(radius * 2.1f, MinimumDistance, _maximumDistance);
            ApplyTransform();
        }

        /// <summary>
        /// Widen the zoom ceiling without moving the camera.
        ///
        /// <para><see cref="Frame"/> also jumps the distance, which is right when a preview replaces
        /// the scene and wrong when the arena curves onto its planet - there the view is continuous,
        /// and the point is to be able to pull back to see the globe the ground is part of, not to be
        /// teleported there.</para>
        ///
        /// <para><paramref name="distantFocus"/> is what the camera orbits once it is far enough out
        /// to be looking at the planet rather than at the ground. Without it, pulling back from an
        /// arena that sits on top of a 500-unit sphere keeps orbiting the arena, so the planet swings
        /// around the edge of the screen instead of sitting in the middle of it.</para>
        /// </summary>
        public void SetRange(
            float maximumDistance, float panLimit, float zoomStep = DefaultZoomStep,
            Vector3? distantFocus = null, float minimumPitch = DefaultMinimumPitch,
            float maximumPitch = DefaultMaximumPitch)
        {
            _maximumDistance = Mathf.Max(DefaultMaximumDistance, maximumDistance);
            _panLimit = Mathf.Max(DefaultPanLimit, panLimit);
            _zoomStep = zoomStep;
            _minimumPitch = minimumPitch;
            _maximumPitch = maximumPitch;
            _hasDistantFocus = distantFocus.HasValue;
            if (distantFocus.HasValue) _distantFocus = distantFocus.Value;
            _distance = Mathf.Min(_distance, _maximumDistance);
            _pitch = Mathf.Clamp(_pitch, _minimumPitch, _maximumPitch);
            ApplyTransform();
        }

        /// <summary>Return to the arena-sized limits, keeping the current viewpoint.</summary>
        public void ResetRange()
        {
            _maximumDistance = DefaultMaximumDistance;
            _panLimit = DefaultPanLimit;
            _zoomStep = DefaultZoomStep;
            _minimumPitch = DefaultMinimumPitch;
            _maximumPitch = DefaultMaximumPitch;
            _hasDistantFocus = false;
            _distance = Mathf.Min(_distance, _maximumDistance);
            _pitch = Mathf.Clamp(_pitch, _minimumPitch, _maximumPitch);
            _focus.x = Mathf.Clamp(_focus.x, -_panLimit, _panLimit);
            _focus.z = Mathf.Clamp(_focus.z, -_panLimit, _panLimit);
            ApplyTransform();
        }

        /// <summary>Restore the arena-sized limits and the arena viewpoint.</summary>
        public void ResetFrame()
        {
            ResetRange();
            _focus = Vector3.zero;
            _distance = Mathf.Min(32f, _maximumDistance);
            ApplyTransform();
        }

        private void Awake()
        {
            _focus = Vector3.zero;
            ApplyTransform();
        }

        private void LateUpdate()
        {
            _distance = Mathf.Clamp(
                _distance - (Input.mouseScrollDelta.y * _distance * _zoomStep), MinimumDistance, _maximumDistance);

            if (Input.GetMouseButton(1))
            {
                // Pan along the camera's own axes, so dragging right moves the view right whichever
                // way it is currently facing.
                Vector3 right = transform.right;
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                var delta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
                _focus -= ((right * delta.x) + (forward * delta.y)) * (_distance * .035f);
                _focus.x = Mathf.Clamp(_focus.x, -_panLimit, _panLimit);
                _focus.z = Mathf.Clamp(_focus.z, -_panLimit, _panLimit);
            }

            if (Input.GetMouseButton(0))
            {
                // Yaw as well as pitch. Without it the view has one fixed compass direction, which is
                // survivable for a 50-unit square and useless for a planet - the far side simply
                // cannot be looked at.
                _yaw += Input.GetAxisRaw("Mouse X") * 5f;
                _pitch = Mathf.Clamp(_pitch - (Input.GetAxisRaw("Mouse Y") * 5f), _minimumPitch, _maximumPitch);
            }

            ApplyTransform();
        }

        private void ApplyTransform()
        {
            // Hand over from orbiting the ground to orbiting the planet as the camera retreats. The
            // handover runs across the range between the arena's own ceiling and the new one, so
            // close in nothing changes and far out the planet is centred rather than off the edge.
            Vector3 focus = _focus;
            if (_hasDistantFocus && _maximumDistance > DefaultMaximumDistance)
            {
                float t = Mathf.InverseLerp(DefaultMaximumDistance, _maximumDistance, _distance);
                focus = Vector3.Lerp(_focus, _distantFocus, Mathf.SmoothStep(0f, 1f, t));
            }

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.position = focus + (rotation * new Vector3(0f, 0f, -_distance));
            transform.rotation = rotation;
        }
    }
}
