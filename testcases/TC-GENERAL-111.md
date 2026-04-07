# TC-GENERAL-111: Test Serialization Configuration with Unity's built-in JsonUtility

- **Requirement:** REQ-GENERAL-034
- **Type:** Unit
- **Risk:** Medium
- **Created:** 2026-03-22 13:54:51

## Precondition

A class that uses `JsonUtility` for serialization is created.

## Steps

1. Create a new C# script and use `JsonUtility` to serialize and deserialize data types which are not directly supported by `JsonUtility` like Nullable types and Collection types.
2. Verify the result of the serialization process using various test cases.

## Expected Result

The system uses Unity's built-in `JsonUtility` for save data without using Newtonsoft or System.Text.Json at runtime. Nullable types are handled with a bool + value pattern and Collection types like HashSet, Dictionary can be serialized and deserialized correctly.
