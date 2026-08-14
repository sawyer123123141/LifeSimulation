# Ground-Plane Camera Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add smooth, bounded ground-plane camera pan, zoom, and pitch controls without changing simulation truth.

**Architecture:** A single presentation-only MonoBehaviour tracks focus, distance, and pitch. It uses fixed yaw and `LateUpdate`; `Prototype1Presenter` ensures the controller is attached to the active main camera.

### Task 1: Add the camera controller

**Files:**
- Create: `Assets/Scripts/Presentation/GroundPlaneCameraController.cs`
- Modify: `Assets/Scripts/Presentation/Prototype1Presenter.cs`

- [ ] Implement scroll zoom clamped to `[8, 60]`, right-drag pan clamped to the arena with margin, and left-drag vertical pitch clamped to `[25, 75]` degrees.
- [ ] Preserve yaw, make plain left clicks no-ops, and apply the transform in `LateUpdate`.
- [ ] Ensure the presenter attaches/configures the controller once on the main camera.
- [ ] Compile through Unity, manually verify all three controls, run EditMode tests, and commit `feat: add ground plane camera controls`.
