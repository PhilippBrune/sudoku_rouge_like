# Privacy And Analytics Notice

## Current Release Baseline

This project does not include Unity Analytics or another platform telemetry package in `Packages/manifest.json`. The current release baseline is analytics-free.

The game code records local, in-memory run summaries for end-of-run screens, such as puzzle mistakes, item use, and completion statistics. These local gameplay summaries are used inside the game session and are not transmitted by a platform analytics service.

## Release Requirement

Before any public store, Steam, early-access, beta, or release build:

- Keep `Packages/manifest.json` free of `com.unity.analytics`, `com.unity.services.analytics`, and `com.unity.modules.unityanalytics` unless a new product decision reintroduces telemetry.
- If platform analytics is reintroduced, add final privacy policy text, player disclosure, consent handling where required, data retention notes, store-page privacy answers, and localized legal copy before release.

With the current analytics-free baseline, `PlatformServiceRegistry.CurrentStatus` reports the privacy/analytics notice as available and does not add a privacy release blocker.

## Player-Facing Credit Notice

The Credits screen and `docs/Credits.md` must reference this notice so testers and release reviewers can find the current privacy/analytics status from inside the legal flow.
