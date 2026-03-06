# Retired Content

This folder stores code/assets intentionally retired from active Unity import paths.

- `nested-script-tree/` contains the old duplicate script tree that previously lived under:
  - `Assets/Scripts/My project/Assets/Scripts`

Rationale:
- Top-level `Assets/Scripts` is now the single source of truth.
- Keeping retired content outside `Assets` prevents duplicate class compilation in Unity.
