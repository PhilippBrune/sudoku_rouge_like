# Beta test keys (10 free keys for testers)

Guide · 2026-10-18 · for the owner. Source: Steamworks documentation ["Steam Keys"](https://partner.steamgames.com/doc/features/keys) and ["Branches"](https://partner.steamgames.com/doc/store/application/branches).

## How it works

Steam has a key type for exactly this: **Release State Override** keys (also called beta keys). They give access to the game **before release**; a tester activates the key and can download and play at once. Testers get the build that is live on the **default branch** (before release only you and key holders see it). Keys do not expire; after the beta you can keep them as a thank-you or ban them.

Rules from Valve: beta keys are free (never sold), each request is reviewed by Valve, about 2,500 of this type in total, and for your first game you must wait three weeks after the App ID was created before you can request keys. For a large open test, Steam Playtest is the better tool (sign-ups on the store page, no keys).

## Steps

1. **A build worth testing.** Build 25938214 (v0.1.5) is uploaded but older than the latest fixes and the new screens. Make a new build and upload it, then set it live on the default branch (Steamworks → App Admin → SteamPipe → Builds → "Set build live for branch… default" → Preview → Set Build Live Now). Before release this only affects you and key holders. If you want testers on a separate version, create a password-protected branch instead and give them the password; they choose it under Properties → Game Versions & Betas.
2. **Request the keys.** Steamworks → the Run of the Nine landing page → **Request Steam Product Keys** (needs the "Generate Steam Keys" permission) → type **Release State Override** → the game's package → amount **10** → a tag such as `beta-2026-10` → note "closed beta for 10 community testers" → submit.
3. **Download them** once Valve has approved: the same button; you get a zip with a text file, and only you can download it. Keep the file and the list of who got which key in a private place (password manager or a private spreadsheet), **never in this repo or a Discord channel**.
4. **Pick the testers** on Discord: post a call in `#dev-log` or `#polls` ("10 beta spots: reply in the thread"), choose people who will actually report (different hardware, controller and keyboard players, a few languages).
5. **Send one key per person by DM**, with a short note: how to activate (Steam → Games → Activate a Product on Steam → enter the key), where to report (`#rotn-bugs`, one post per bug), what to test this round, and that keys are personal (no reselling, no sharing).
6. **Give them the Playtester role** (opens the playtest channels and names them in the game's credits after you ask them; `discord_admin.py export-credits`).
7. **After the beta:** thank them; keep the keys active (nice for testers) or ban the batch in Steamworks (Ban Steam Keys tool) if a key leaked.

## What I can prepare

- A `#playtest` channel only Playtesters see, with a pinned test plan and a bug-report template (`layout.json` + `apply`).
- The tester call and the DM text as drafts in `#release-drafts`.
- A new player build from the mirror for you to upload (the upload and "set live" stay your steps).
