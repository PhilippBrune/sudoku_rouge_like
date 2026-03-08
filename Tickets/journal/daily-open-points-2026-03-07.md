# Daily Open Points - 2026-03-07

## From JIRA-0006
- Runtime validation in Unity Play Mode pending for region variant selection (verify different layouts appear for same board size across levels).
- Verify jigsaw (variant 2) templates render correctly in-game for 6×6, 8×8, and 9×9 boards.
- Consider adding more jigsaw template variants (variant 3+) for additional variety in future.

## From JIRA-0005 (carried over)
- Runtime validation pass in Unity Play Mode still pending for shop reroll spend logic, item list interaction, and run SFX triggers.

## From JIRA-0007
- Create `Assets/Scenes/MainMenu.unity` and `Assets/Scenes/Prototype.unity` in the Unity Editor (cannot be done from code — requires manual Editor work).
- Set build indices: MainMenu = 0, Prototype = 1 in File → Build Profiles → Scene List.

## Unity Performance
- Mirror tree folders already removed — Unity loads quickly now.
