# TC-TUTORIAL-049: TestUIManagerDropdownValuesMatchSettings

- **Requirement:** REQ-TUTORIAL-048
- **Type:** Unit
- **Risk:** High
- **Created:** 2026-03-22 11:39:11

## Precondition

UIManager is instantiated and settings defined in TutorialSetupConfig object.

## Steps

1. Setup a new TutorialSetupConfig with desired values for board/stars/class/region selection and modifier toggles.
2. Instantiate the UIManager using this config.
3. Verify that all UI elements corresponding to these settings are correctly updated in dropdowns.

## Expected Result

All dropdowns display matching values as defined by TutorialSetupConfig object.
