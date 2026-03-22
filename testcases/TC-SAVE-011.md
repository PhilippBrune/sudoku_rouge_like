# TC-SAVE-011: Test Backup Rotation Policy Implementation

- **Requirement:** REQ-SAVE-011
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 08:55:48

## Precondition

System is installed and running without backup rotation policy implemented.

## Steps

1. Start a new game session.
2. Save the game.
3. Repeat steps 2-4 five times, creating more than five save files in total.
4. Verify that only five save files are present after step 3 and all of them have timestamps associated with them indicating they were saved at a specific time.

## Expected Result

All but the oldest backup file(s) should be deleted to adhere to the policy. The five most recent backups should remain intact.
