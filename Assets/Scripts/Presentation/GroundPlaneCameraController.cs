using UnityEngine;

namespace LifeSimulation.Presentation
{
    public sealed class GroundPlaneCameraController : MonoBehaviour
    {
        private const float MinimumDistance = 8f;
        private const float DefaultMaximumDistance = 60f;
        private const float DefaultPanLimit = 35f;
        /// <summary>
        /// Fraction of the current distance one wheel notch covers. Zoom is multiplicative, so the
        /// number of notches between two distances is <c>ln(far/near) / ln(1 + step)</c> - at the
        /// arena's 0.12 that is thirty-four notches from 32 units out to a 500-unit planet, which is
        /// not a zoom control, it is a chore. Callers showing something that large pass a coarser
        /// step.
        /// </summary>
        private const float DefaultZoomStep = 0.12f;

        private const float MinimumPitch = 25f;
        private const float MaximumPitch = 75f;
        private Vector3 _focus;
        private float _distance = 32f;
        private float _pitch = 52f;
        private float _maximumDistance = DefaultMaximumDistance;
        private float _panLimit = DefaultPanLimit;
        private float _zoomStep = DefaultZoomStep;

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
        /// </summary>
        public void SetRange(float maximumDistance, float panLimit, float zoomStep = DefaultZoomStep)
        {
            _maximumDistance = Mathf.Max(DefaultMaximumDistance, maximumDistance);
            _panLimit = Mathf.Max(DefaultPanLimit, panLimit);
            _zoomStep = zoomStep;
            _distance = Mathf.Min(_distance, _maximumDistance);
        }

        /// <summary>Return to the arena-sized zoom ceiling, keeping the current viewpoint.</summary>
        public void ResetRange()
        {
            _maximumDistance = DefaultMaximumDistance;
            _panLimit = DefaultPanLimit;
            _zoomStep = DefaultZoomStep;
            _distance = Mathf.Min(_distance, _maximumDistance);
            _focus.x = Mathf.Clamp(_focus.x, -_panLimit, _panLimit);
            _focus.z = Mathf.Clamp(_focus.z, -_panLimit, _panLimit);
            ApplyTransform();
        }

        /// <summary>Restore the arena-sized limits.</summary>
        public void ResetFrame()
        {
            _maximumDistance = DefaultMaximumDistance;
            _panLimit = DefaultPanLimit;
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
            _distance = Mathf.Clamp(_distance - (Input.mouseScrollDelta.y * _distance * _zoomStep), MinimumDistance, _maximumDistance);
            if (Input.GetMouseButton(1))
            {
                Vector3 right = transform.right;
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                Vector2 delta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
                _focus -= (right * delta.x + forward * delta.y) * (_distance * .035f);
                _focus.x = Mathf.Clamp(_focus.x, -_panLimit, _panLimit);
                _focus.z = Mathf.Clamp(_focus.z, -_panLimit, _panLimit);
            }

            if (Input.GetMouseButton(0)) _pitch = Mathf.Clamp(_pitch - (Input.GetAxisRaw("Mouse Y") * 5f), MinimumPitch, MaximumPitch);
            ApplyTransform();
        }

        private void ApplyTransform()
        {
            Quaternion rotation = Quaternion.Euler(_pitch, 0f, 0f);
            transform.position = _focus + (rotation * new Vector3(0f, 0f, -_distance));
            transform.rotation = rotation;
        }
    }
}
