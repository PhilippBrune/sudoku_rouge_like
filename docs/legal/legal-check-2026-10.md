# Legal check: Run of the Nine (October 2026)

**Version:** 1.1 | **Date:** 2026-10-22 | **Status:** review (owner's request: "Check if the game violates any laws")

### Change History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.1 | 2026-10-22 | review | GAP-068 completed (owner: "implement GAP-068"): every standalone build gets the `Licenses` folder from an editor build step, and the Credits show the texts (LEGAL-LICENSE-001). The game shows a clean Roboto attribution (`Roboto-ATTRIBUTION.txt`); `Roboto-NOTICE.md` stays the internal note. |
| 1.0 | 2026-10-21 | review | First check of the game, the repository and the release setup against the laws and platform rules that apply to a German developer selling a PC game on Steam worldwide. |

> This is a technical check from the code, the build and the documents, not legal advice. Points marked **Check** need
> the owner (or, where money or liability is involved, a lawyer or tax adviser) to confirm.

## Result

The game itself breaks no law we could find: it collects no personal data, has no real-money randomness, ships no
third-party art or music, and names its licences. One licence duty was not met and is fixed with 0.2.1 (the licence
texts were not in the build). The open points are about the store and the business around the game, not the game.

| Area | Finding | Status |
|---|---|---|
| Privacy (GDPR / DSGVO) | No telemetry, no analytics package, Unity Analytics, crash and performance reporting all off; no network code besides Steamworks; saves stay on the PC (and in Steam Cloud). | OK |
| Steam features and personal data | Achievements, stats, leaderboards (best run score uploaded with the Steam account) and Auto-Cloud are Valve's services under Valve's privacy policy; the game reads no other player's data. | OK, a short note in the store text recommended |
| Gambling and loot boxes | The Koi Gambler and "Double or Quits" risk in-game gold or 1 HP on chance. No real money, nothing to buy, nothing to cash out, so not gambling (GlüStV § 3) and no paid loot boxes. | OK; declare "simulated gambling" in the content survey |
| Age rating | No violence, no real gambling; simulated chance with game gold. | **Check** the Steamworks content survey and, if Steam offers it for the app, a free IARC/USK rating for Germany |
| AI-generated content (Steam rules) | Art made with Fooocus, code and texts with AI assistance (Credits). Valve requires a disclosure of pre-generated AI content in the content survey; it is shown on the store page. | **Check** that the survey for App 4858470 lists art, text and any AI music |
| Licences of bundled code and font | Roboto (Apache 2.0) and Steamworks.NET (MIT) require their licence text to travel with the game. The Credits named them but pointed to repository paths; the 0.2.0 build had no licence files. | **Fixed** (GAP-068): `Licenses/` in every build (automatic since 1.1) and the full texts on the Credits screen |
| AI art and copyright | Images made by a model without enough human creative input are, in the EU and the US, likely not protected by copyright: others could reuse them. Using them is allowed (SDXL's CreativeML Open RAIL++-M licence permits commercial use; Fooocus is GPL software, which does not cover its outputs). | OK; a business risk, not a legal one |
| Music | The shipped music, SFX and UI sounds are project-made synthesis (provenance register). Mureka-made music is not in the game; `audio/music_new` is empty. If Mureka tracks are added, the paid plan's commercial terms apply. | OK |
| Trademarks | "Run of the Nine": no search on record. "Sudoku" is a Nikoli trademark in Japan and generic elsewhere. | **Check** a DPMA/EUIPO search before the full release |
| Accessibility law (EAA / BFSG) | Games are not among the products and services of the European Accessibility Act (in force since 28 June 2025), and micro-enterprises are exempt for services. | Not in scope; the accessibility options are voluntary |
| EU Digital Services Act | Marketplaces must show who the trader is (DSA Art. 30); Steam asks EU developers for their trader status and contact details, shown on the store page. | **Check** the trader status in Steamworks |
| Impressum (§ 5 DDG) | A German business website needs an Impressum; the Discord server and any website about the game count as business offers once the game is sold. | **Check** an Impressum wherever the game is promoted outside Steam |
| Tax and business | Steam pays royalties; income from sales is taxable in Germany, a trade registration (Gewerbe) may be needed, and Steamworks needs the tax form. | **Check** with a tax adviser |
| Secrets in the repository | No tokens or keys in tracked files (scan of 2026-10-21); the Discord bot token lives only in `~/.claude/channels/discord/.env`. | OK |
| Children | No accounts, no chat, no data from players: nothing for COPPA or GDPR Art. 8. | OK |

## Details

### Privacy

- `Packages/manifest.json` has no analytics package; `ProjectSettings/UnityConnectSettings.asset` has Unity Analytics,
  Crash Reporting and Performance Reporting disabled (`m_Enabled: 0`).
- No `UnityWebRequest`, `HttpClient` or other network code in `Assets/Scripts`; the only online part is Steamworks.
- `docs/legal/privacy-analytics-notice.md` states the analytics-free baseline; it stays true for 0.2.1.
- Recommended (not required): one line in the store text, "Achievements, stats, leaderboards and cloud saves use your
  Steam account through Steam; the game collects no data itself."

### Licences shipped with the game (GAP-068)

| Component | Licence | Duty | 0.2.1 |
|---|---|---|---|
| Roboto Regular (`Assets/Resources/Fonts/MainFont.ttf`) | Apache 2.0 | Give recipients a copy of the licence and keep the attribution | `Licenses/APACHE-2.0.txt`, `Licenses/Roboto-ATTRIBUTION.txt` (0.2.1 shipped the internal `Roboto-NOTICE.md` instead) |
| Steamworks.NET (compiled into the game) | MIT | Include the copyright and permission notice with copies | `Licenses/Steamworks.NET-LICENSE.md` |
| Steamworks API (`steam_api64.dll`) | Steamworks SDK Access Agreement | Redistribution allowed for Steam games | nothing to add |
| Google Mobile Dependency Resolver | Apache 2.0 | Editor-only, not in the build | nothing to add |
| Unity runtime | Unity terms | covered by the Unity licence | nothing to add |

Since 1.1 (LEGAL-LICENSE-001) the duty no longer depends on packaging: `LicenseFilesBuildStep` copies the three texts
into `Licenses/` after every standalone build, and the Credits end with a **Licence Texts** section that shows them in
full (bundled in `Resources/Legal`, kept equal to `docs/legal/third-party` by `ThirdPartyLicensesTests`).

### Gambling-like content

- Koi Gambler class, "Double or Quits" (Level 15 item: double the puzzle's gold or lose 1 HP), shop and reward rolls.
- All stakes are in-game resources; there is no purchase in the game, no currency for money and no trading. Under the
  German Glücksspielstaatsvertrag (§ 3) gambling needs a paid stake; the Belgian and Dutch loot-box rulings concern paid
  boxes. None applies.
- Rating systems still list "simulated gambling" as content: declare it in the Steam content survey.

## Actions for the owner

1. Steamworks, App 4858470: content survey (AI-generated content: art, text, any music; simulated gambling), trader
   status (DSA), and, if offered, an IARC/USK rating. Screenshots before saving, as usual.
2. Before the full release: trademark search for "Run of the Nine"; an Impressum on any website or public page.
3. Tax adviser: business registration and the tax side of Steam income.
4. Nothing to do for the licence texts any more: every build carries them (since 1.1).
