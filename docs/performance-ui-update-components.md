# UI Update Helper Components

`InRunController.Update()` remains the high-level in-run coordinator, but low-level polling and view-state decisions should live in focused helpers when they are stable enough to test outside a live scene.

Current extracted helpers:

- `ControllerIndicatorPoller`: owns the controller-indicator poll interval and cached joystick count so `Input.GetJoystickNames()` is not queried every frame.
- `AsyncLevelCompletionPresenter`: converts completed async boss-generation state into a view result for the in-run controller.
- `TimedVisualStateController`: centralizes positive realtime-expiry checks used by temporary visual effects such as Garden Lantern reveal.

Guidelines for future per-frame work:

- Keep `Update()` orchestration readable: poll input, delegate focused decisions, then update the active screen.
- Prefer pure helper classes for timing, formatting, and state transitions that can be covered by EditMode tests.
- Avoid moving scene-object mutation into helpers unless the helper owns that UI surface.
- Add a test whenever a helper controls a timer, retry window, fallback path, or localized status string.
- Use Unity Profiler before broad rewrites of HUD refresh, dynamic panel rebuilds, audio updates, or path particles.

Runtime validation still needs a Unity Editor pass for the in-run scene because the local command-line Unity run is not available in this environment.
