using UnityEngine;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// Free-fly developer camera: a position and a direction, and nothing else.
    ///
    /// <para><b>Hold the right mouse button to fly.</b> The mouse looks, WASD moves along the view
    /// axes, Q and E go down and up, shift boosts, the wheel sets the speed. With the button up the
    /// camera is inert, so every scenario hotkey - including D, E and F - still means what it always
    /// meant. Arrow keys move without the button, and the wheel alone dollies. Home returns to the
    /// arena.</para>
    ///
    /// <para><b>Why this replaced the orbit rig.</b> An orbit camera can only look at the point it
    /// is orbiting, so reaching anything meant zoom-to-cursor, then pan, then re-orbit, and the pan
    /// had to be clamped to a box in x and z - a shape that is wrong everywhere on a sphere except
    /// the arena's centre. Four bugs in a row came out of that clamp and out of the two focus rules
    /// that had to hand over to each other as the camera retreated. A free camera has no focus and
    /// no orbit, so it has neither rule and neither bug: there is nothing to clamp but a height.</para>
    ///
    /// <para>Speed is proportional to height above the surface, which is what makes one camera work
    /// beside a 1-unit creature and around a 500-unit planet. See <see cref="FreeCameraMotion"/>,
    /// where that arithmetic lives so it can be tested.</para>
    /// </summary>
    public sealed class FreeFlyCameraController : MonoBehaviour
    {
        /// <summary>Arena half-width plus a margin: the scale the camera starts at.</summary>
        private const float ArenaExtent = 50f;

        /// <summary>Distance the camera sits back from the arena, and the angle it looks down at.</summary>
        private const float ArenaDistance = 32f;
        private const float ArenaPitch = 52f;

        /// <summary>Degrees of look per unit of mouse movement.</summary>
        private const float LookSensitivity = 4.5f;

        /// <summary>Units per wheel notch when dollying, as a fraction of the current speed.</summary>
        private const float DollyNotch = 1.6f;

        /// <summary>Centre of the body being looked at, and its surface radius.</summary>
        private Vector3 _centre = Vector3.zero;

        /// <summary>
        /// Radius of the surface height is measured from. Zero means the world is a plane and height
        /// is simply the y coordinate - which is the case for the arena and for every flat preview.
        /// One rule either way; there is deliberately no blend between them, because a handover
        /// between two framing rules is where the last camera's worst bugs came from.
        /// </summary>
        private float _surfaceRadius;

        /// <summary>Radius of what is on screen. Sets the speed ceiling and the height ceiling.</summary>
        private float _extent = ArenaExtent;

        /// <summary>
        /// The world's own framing, as opposed to whatever is being previewed on top of it.
        ///
        /// <para><see cref="Frame"/> overrides the three fields above while a preview is on screen;
        /// these remember what to go back to. Without them, returning from a preview while the arena
        /// is curved would restore the flat rules and the planet would be measured as if it were a
        /// ground plane.</para>
        /// </summary>
        private Vector3 _worldCentre = Vector3.zero;
        private float _worldSurfaceRadius;
        private float _worldExtent = ArenaExtent;

        /// <summary>Wheel-adjusted speed multiplier, kept across framings because it is a preference.</summary>
        private float _dial = 1f;

        /// <summary>Look direction, in degrees above the local horizon.</summary>
        private float _pitch;

        /// <summary>
        /// Point the camera at something of a given size and pull back far enough to see it.
        ///
        /// <para>Used when a preview replaces the scene. The preview sits at the origin and is looked
        /// at as a flat world regardless of what the arena is doing, because it is a separate object
        /// on a turntable rather than part of the planet.</para>
        /// </summary>
        public void Frame(float radius)
        {
            _centre = Vector3.zero;
            _surfaceRadius = 0f;
            _extent = Mathf.Max(ArenaExtent, radius);
            PlaceForFraming(radius * 2.1f);
        }

        /// <summary>
        /// Tell the camera what it is flying around, without moving it.
        ///
        /// <para>Called when the arena curves onto its planet. The view is continuous there - the
        /// point is to be able to fly off and see the globe the ground is part of, not to be
        /// teleported to it - so only the scale changes. After this, height is measured from the
        /// sphere and up points away from its centre, so flying to the far side stays upright
        /// instead of arriving upside down.</para>
        /// </summary>
        public void SetExtent(float radius, Vector3 centre, float surfaceRadius)
        {
            _worldCentre = centre;
            _worldSurfaceRadius = surfaceRadius;
            _worldExtent = Mathf.Max(ArenaExtent, radius);
            _centre = _worldCentre;
            _surfaceRadius = _worldSurfaceRadius;
            _extent = _worldExtent;
        }

        /// <summary>Return to the arena's scale, keeping the current viewpoint.</summary>
        public void ResetExtent()
        {
            _worldCentre = Vector3.zero;
            _worldSurfaceRadius = 0f;
            _worldExtent = ArenaExtent;
            _centre = _worldCentre;
            _surfaceRadius = _worldSurfaceRadius;
            _extent = _worldExtent;
        }

        /// <summary>Return to the arena's scale and its viewpoint. Bound to Home.</summary>
        public void ResetFrame()
        {
            _centre = _worldCentre;
            _surfaceRadius = _worldSurfaceRadius;
            _extent = _worldExtent;
            PlaceForFraming(ArenaDistance);
        }

        private void Awake()
        {
            ResetFrame();
        }

        private void LateUpdate()
        {
            bool flying = Input.GetMouseButton(1);
            float scroll = Input.mouseScrollDelta.y;

            if (flying && Mathf.Abs(scroll) > 0f) _dial = FreeCameraMotion.AdjustDial(_dial, scroll);

            if (flying)
            {
                Look(
                    Input.GetAxisRaw("Mouse X") * LookSensitivity,
                    Input.GetAxisRaw("Mouse Y") * LookSensitivity);
            }

            float speed = FreeCameraMotion.SpeedAt(
                Altitude(transform.position), _extent, _dial,
                boost: Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
                slow: Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));

            Vector3 move = ReadMovement(flying);
            Vector3 position = transform.position + (move * (speed * Time.unscaledDeltaTime));

            // Wheel dollies when not flying, so the wheel still does the obvious thing for someone
            // who has not touched the right button yet.
            if (!flying && Mathf.Abs(scroll) > 0f) position += transform.forward * (scroll * speed * DollyNotch);

            transform.position = ClampPosition(position);

            if (Input.GetKeyDown(KeyCode.Home)) ResetFrame();

            // Rebuild the rotation every frame rather than integrating it. Up is whatever up is here,
            // so roll can never accumulate and there is no orientation state to drift.
            ApplyRotation(transform.forward);
        }

        /// <summary>Which way is up at a point: away from the planet's centre, or simply +Y.</summary>
        private Vector3 UpAt(Vector3 position)
        {
            if (_surfaceRadius <= 0f) return Vector3.up;
            Vector3 radial = position - _centre;
            return radial.sqrMagnitude < 1e-6f ? Vector3.up : radial.normalized;
        }

        /// <summary>Height above the nominal surface - the sphere's, or the ground plane's.</summary>
        private float Altitude(Vector3 position)
        {
            if (_surfaceRadius <= 0f) return position.y - _centre.y;
            return Vector3.Distance(position, _centre) - _surfaceRadius;
        }

        private void Look(float yawDegrees, float pitchDegrees)
        {
            Vector3 up = UpAt(transform.position);
            Vector3 forward = Quaternion.AngleAxis(yawDegrees, up) * transform.forward;

            float pitch = Mathf.Asin(Mathf.Clamp(Vector3.Dot(forward.normalized, up), -1f, 1f)) * Mathf.Rad2Deg;
            _pitch = FreeCameraMotion.ClampPitch(pitch, pitchDegrees);

            // Negative because a positive rotation about the right-hand axis tips the view down.
            Vector3 right = Vector3.Cross(up, forward);
            if (right.sqrMagnitude > 1e-8f)
            {
                forward = Quaternion.AngleAxis(-(_pitch - pitch), right.normalized) * forward;
            }

            ApplyRotation(forward);
        }

        private void ApplyRotation(Vector3 forward)
        {
            Vector3 up = UpAt(transform.position);
            Vector3 flattened = Vector3.ProjectOnPlane(forward, up);

            // Straight up or straight down leaves nothing to align the horizon with. The pitch clamp
            // stops short of vertical so this only guards against a caller-supplied direction.
            if (flattened.sqrMagnitude < 1e-8f) return;

            transform.rotation = Quaternion.LookRotation(forward.normalized, up);
        }

        /// <summary>
        /// Unit movement direction from the keys.
        ///
        /// <para>WASD, Q and E are read only while the right button is held. They are also scenario
        /// hotkeys - D resets to the drought scenario, E to the starter habitat - and a camera that
        /// swallowed them would have cost more than it gained. The arrow keys are bound to nothing
        /// else and so move freely.</para>
        /// </summary>
        private Vector3 ReadMovement(bool flying)
        {
            Vector3 up = UpAt(transform.position);
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            Vector3 move = Vector3.zero;

            if (Input.GetKey(KeyCode.UpArrow)) move += forward;
            if (Input.GetKey(KeyCode.DownArrow)) move -= forward;
            if (Input.GetKey(KeyCode.RightArrow)) move += right;
            if (Input.GetKey(KeyCode.LeftArrow)) move -= right;

            if (flying)
            {
                if (Input.GetKey(KeyCode.W)) move += forward;
                if (Input.GetKey(KeyCode.S)) move -= forward;
                if (Input.GetKey(KeyCode.D)) move += right;
                if (Input.GetKey(KeyCode.A)) move -= right;
                if (Input.GetKey(KeyCode.E)) move += up;
                if (Input.GetKey(KeyCode.Q)) move -= up;
            }

            return move.sqrMagnitude < 1e-8f ? Vector3.zero : move.normalized;
        }

        /// <summary>Push the position back inside the allowed band of heights, along the local up.</summary>
        private Vector3 ClampPosition(Vector3 position)
        {
            float altitude = Altitude(position);
            float clamped = FreeCameraMotion.ClampAltitude(altitude, _extent);
            if (Mathf.Approximately(altitude, clamped)) return position;

            return position + (UpAt(position) * (clamped - altitude));
        }

        /// <summary>
        /// Sit back from the origin and look at it, down the arena's usual angle.
        ///
        /// <para>The origin, and not <see cref="_centre"/>: the arena and every preview sit there,
        /// while the planet's centre is five hundred units underground and framing on it would put
        /// the camera inside the world.</para>
        /// </summary>
        private void PlaceForFraming(float distance)
        {
            Quaternion rotation = Quaternion.Euler(ArenaPitch, 0f, 0f);
            transform.position = rotation * new Vector3(0f, 0f, -Mathf.Max(distance, 1f));
            transform.rotation = rotation;
            _pitch = -ArenaPitch;
        }
    }
}
