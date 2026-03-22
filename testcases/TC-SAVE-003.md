# TC-SAVE-003: Backup Rotation Policy Test

- **Requirement:** REQ-SAVE-004
- **Type:** Integration
- **Risk:** High
- **Created:** 2026-03-22 08:55:23

## Precondition

A game save system that allows automatic backups and limits the number of them to five

## Steps

1. Trigger a game save for the sixth time.
2. Check if the oldest backup is correctly deleted after saving the sixth run.

## Expected Result

The fifth save should not be present after the sixth save, while all others should still exist.
