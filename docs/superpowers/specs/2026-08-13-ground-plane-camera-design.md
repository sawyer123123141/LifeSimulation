# Ground-Plane Camera Design

The Unity camera is presentation only: it reads no simulation state and never writes positions, time, seeds, or decisions. A `GroundPlaneCameraController` owns a ground-plane focus point, pitch, and zoom distance. Yaw is fixed, so screen-up is always world north.

Controls: mouse wheel adjusts clamped zoom; hold right mouse and drag pans the focus point along the camera's ground-plane right/forward vectors; hold left mouse and drag vertically adjusts pitch only. A small drag threshold leaves an un-dragged left click available for future creature/resource selection. The controller clamps focus to a configured simulation arena plus presentation margin, pitch to a readable oblique range, and distance to useful near/far bounds.

The controller uses `LateUpdate`, applies no allocations per frame, and can be attached to the existing main camera. It does not introduce Cinemachine or change the simulation's 2D ground-plane contract.
