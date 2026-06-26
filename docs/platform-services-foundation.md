# Platform Services Foundation

## Current Scope

The runtime now exposes a small platform capability layer:

- `Assets/Scripts/Platform/PlatformServices.cs`
- `Assets/Scripts/Meta/SteamAchievementService.cs`
- `Assets/Scripts/Meta/PlatformAchievementBridge.cs`

This layer is intentionally a readiness/status abstraction. It does not bundle
Steamworks, does not enable cloud save, does not enable platform telemetry, and
does not claim platform release support by itself.

## Capability States

Platform features report one of:

- `Available`: runtime provider is present and callable.
- `Unavailable`: provider exists but is temporarily unavailable.
- `NotConfigured`: no provider is configured in this repository/build.

`PlatformServiceRegistry.CurrentStatus` reports:

- Achievements through `SteamAchievementService.GetPlatformAchievementReport()`.
- Cloud save through `LocalOnlyCloudSaveProvider`.
- Privacy/analytics notice status, currently `Available` because Unity
  Analytics and other platform telemetry packages are absent.
- Release blockers derived from non-available capabilities.

## Cloud Save

Cloud save remains explicitly local-only. `LocalOnlyCloudSaveProvider` reports
`NotConfigured` so future Steam Cloud or platform sync work can be added without
pretending the current save system is cloud-backed.

Before implementing cloud save, define:

1. Provider target such as Steam Cloud.
2. Conflict strategy for multiple devices.
3. Schema/version migration behavior.
4. Offline behavior.
5. User-facing conflict and recovery UI.

## Release Use

Use the platform status layer to drive development checks and future UI, but do
not show it as a player-facing platform support claim until:

1. The SDK/plugin is imported.
2. Platform runtime validation is complete.
3. Legal/privacy notices are complete.
4. Store assets and metadata are ready.
