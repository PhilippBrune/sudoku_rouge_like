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

## Player feedback (since 0.4.3, FEEDBACK-001/002)

The feedback form (F8, or Options › Run › Send feedback) sends a report only when the player presses Send, and only the
parts the player ticked: the note and the optional Discord name, the screenshot taken when the form opened, the run's
details (seed, board, class, floor, HP, gold, items, relics), the last 200 lines of the game's log with the Windows user
folder and user name replaced, and the save file (unticked at first). A random ID made once per installation goes with
it so the relay can limit reports (5 an hour); no Steam ID and no account name are sent. The report goes to a Cloudflare
Worker run by the developer, which posts it to a private Discord channel of the studio; the Worker keeps only a counter
per installation ID for two hours. Offline, or before the Worker is set up, the report is saved as a ZIP in the game's
data folder and leaves the PC only if the player posts it.
