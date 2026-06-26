# Background Asset Budget

## Purpose
This file closes the ASSET-004 review gap by turning large runtime backgrounds into a tracked release budget instead of an unbounded asset folder. The current game ships fullscreen UI and scene backgrounds from `Assets/Resources/background`, so these files affect import time, build size, memory, and loading behavior.

## Current Inventory
- Runtime backgrounds: 39 PNG files.
- Current total source size: about 132 MB.
- Largest current source PNG: about 8.4 MB.
- Prompt/reference text files have been moved to `docs/art-source/background` so they are tracked as provenance without becoming runtime Resources content.

## Enforced Budgets
- Per-background source PNG budget: 10 MB.
- Total runtime background source PNG budget: 150 MB.
- Unity import metadata must keep:
  - `isReadable: 0`
  - `enableMipMap: 0`
  - `maxTextureSize: 2048`
  - `textureCompression: 1`

These limits are intentionally conservative for the current desktop target. They prevent accidental regressions while leaving enough space for the existing generated art.

## Validation
`BackgroundAssetBudgetTests` verifies the source-size budget and reviewed Unity import settings for every `.png` directly under `Assets/Resources/background`.

## Manual Release Review
Before a release candidate, perform a Unity player build and inspect:

- final texture memory in the profiler;
- actual platform compression format after import;
- scene transition loading time for background-heavy menu screens;
- visual quality at 1920x1080 and 2560x1440;
- whether any background should move out of `Resources` into Addressables or another explicit loading path.

If any image exceeds the budget because it is intentionally higher fidelity, document the exception here and adjust the automated threshold only after profiling.
