# TC-TUTORIAL-204: Tutorial Implementation References Specific Files and Data Model

- **Requirement:** REQ-TUTORIAL-202
- **Type:** Manual
- **Risk:** High
- **Created:** 2026-03-22 13:54:27

## Precondition

User is exploring the tutorial features in detail.

## Steps

1. Look at the codebase for references to specific key files like 'TutorialModeService', 'TutorialProgressService' etc.
2. Investigate how these services handle board sizes, stars, modifiers availability, validation & descriptions.
3. Check the implementation of 'TutorialMenuController' that wires up dropdowns and progress display in UI.
4. Verify 'RunDirector' that isolates tutorial environment from normal gameplay.
5. Confirm how 'ProfileService' stores tutorial progress state.

## Expected Result

The system should reference specific key files, use a data model to track the progress of tutorials, save and load progress correctly for persistent users, and separate tutorial progression from main game without interfering with other systems.
