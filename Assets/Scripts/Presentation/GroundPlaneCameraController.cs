using UnityEngine;

namespace LifeSimulation.Presentation
{
    public sealed class GroundPlaneCameraController : MonoBehaviour
    {
        private const float MinimumDistance = 8f;
        private const float MaximumDistance = 60f;
        private const float MinimumPitch = 25f;
        private const float MaximumPitch = 75f;
        private Vector3 _focus;
        private float _distance = 32f;
        private float _pitch = 52f;

        private void Awake()
        {
            _focus = Vector3.zero;
            ApplyTransform();
        }

        private void LateUpdate()
        {
            _distance = Mathf.Clamp(_distance - (Input.mouseScrollDelta.y * 3f), MinimumDistance, MaximumDistance);
            if (Input.GetMouseButton(1))
            {
                Vector3 right = transform.right;
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                Vector2 delta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
                _focus -= (right * delta.x + forward * delta.y) * (_distance * .035f);
                _focus.x = Mathf.Clamp(_focus.x, -35f, 35f);
                _focus.z = Mathf.Clamp(_focus.z, -35f, 35f);
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
