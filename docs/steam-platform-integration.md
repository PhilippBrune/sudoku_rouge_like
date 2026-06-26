# Steam Platform Integration

## Current Status

The repository does not bundle a Steamworks SDK or native Steam plugin. Achievement progress is persisted locally through `SteamAchievementService`.

A platform bridge is now present:

- `Assets/Scripts/Meta/SteamAchievementService.cs`
- `Assets/Scripts/Meta/PlatformAchievementBridge.cs`
- `Assets/Scripts/Platform/PlatformServices.cs`

The bridge detects a Steamworks-compatible runtime assembly by reflection and forwards local achievement unlock IDs when the SDK is present. If no SDK is imported, local achievement tracking continues to work and platform forwarding is skipped.

`PlatformServiceRegistry.CurrentStatus` exposes achievement availability,
cloud-save status, privacy/analytics status, and release blockers. This is a
status surface only; it does not bundle Steamworks, enable cloud save, or enable
platform telemetry.

## Supported Bridge Shape

The current bridge targets the common Steamworks.NET shape:

```text
Steamworks.SteamUserStats.SetAchievement(string achievementId)
Steamworks.SteamUserStats.StoreStats()
```

If a `SteamManager.Initialized` static bool property is available, the bridge requires it to be true before forwarding.

## Import Requirements

Before claiming Steam release support, add the chosen Steamworks Unity integration and verify:

1. The SDK/plugin is present under `Assets/` or `Packages/`.
2. License and third-party notices are added to `docs/Credits.md`.
3. Native plugin import settings are correct for target platforms.
4. `SteamAchievementService.PlatformBridgeAvailable` returns true in a Steam-launched build.
5. Unlock IDs match the Steam partner backend configuration.
6. `SteamAchievementService.SyncUnlockedAchievements()` is called after profile load or platform login if old local unlocks need to be pushed.
7. Achievement unlocks and `StoreStats()` are validated in a real Steam runtime environment.
8. `PlatformServiceRegistry.CurrentStatus.Achievements.State` reports `Available`
   in the target build.

## Local Achievement IDs

Current IDs forwarded by `SteamAchievementService`:

- `harmony_1_win`
- `harmony_3_win`
- `harmony_5_win`
- `harmony_7_win`
- `harmony_9_win`
- `harmony_10_win`
- `harmony_10_perfect`
- `harmony_all_classes`

## Release Notes

Do not credit or market the build as including the Steamworks SDK until the SDK files, license notices, platform configuration, and runtime validation are present.
