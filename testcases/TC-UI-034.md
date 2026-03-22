# TC-UI-034: Test AudioMixingAndVolumeControls functionality

- **Requirement:** REQ-UI-034
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 12:38:05

## Precondition

The player has started a new game session and audio settings are turned on

## Steps

1. Open the in-game settings menu
2. Navigate to "Audio" section and check the master volume slider
3. Mute/Unmute some of the sounds using AudioSettingsModel
4. Verify if muting an effect mutes that particular sound, and vice versa

## Expected Result

The audio should adjust according to user settings when the volume is changed or effects are turned on/off.
