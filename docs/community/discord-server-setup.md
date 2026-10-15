# Discord server for Run of the Nine and Corebound

Setup guide · 2026-10-14 · for the owner (Philipp)

This guide sets up one Discord server for both games, with roles, a channel per purpose, a release-notes flow, and a bot through which Claude (in Claude Code) can read and post messages and do admin work for you. Steps marked **You** need your Discord account; steps marked **Claude** I do once the bot is in the server.

## 0. Decisions

**One studio server, one category per game.** Both games come from you, so one server shares the announcements, moderation, rules and the bot, and players of one game see the other. Each player picks the game(s) they play when they join (Discord Onboarding), which gives them that game's role and channels. Two separate servers would double the moderation and setup work; split later only if one game's community gets large.

**Claude takes part through a bot, not through your account.** Discord's Terms forbid automating a user account ("self-bots"); accounts get banned for it, and I also must not handle your password or 2FA codes. A bot application is the supported way:

- **Chat**: the official Claude Code Discord plugin (research preview) lets you message me from Discord, and I answer there. It can read and send messages, react, edit its own messages, read history and download attachments, and nothing else.
- **Admin**: for channels, roles, permissions, forum tags, webhooks and posts, I use Discord's API with the same bot token, through a small script in this repository. The token stays on your PC (`~/.claude/channels/discord/.env`) and is never shown in chat or committed.

So the admins are you (server owner) and the bot's role. The bot gets the permissions listed in step 1.5. Discord also offers **Administrator** for a bot: simpler, but a leaked token then means a full server takeover. Start with the list; I'll tell you if something needs more.

## 1. Your steps (account only)

1. **Two-factor authentication** on your Discord account (User Settings → My Account → Enable Two-Factor Auth). Needed later for "Require 2FA for moderator actions".
2. **Create the server**: "+" → Create My Own → For a club or community. Name it after the studio (for example "Kratossos") and use a game logo from `steam_assets/` as the icon. Leave the default channels; I replace them.
3. **Developer Mode**: User Settings → Advanced → Developer Mode on. Right-click the server icon → Copy Server ID, and send me the ID (it is not secret).
4. **Create the bot** at https://discord.com/developers/applications → New Application (for example "Kratossos Bot"):
   - **Bot** tab: set a username and icon. Click **Reset Token** and keep the token for step 6 (Discord shows it once). Turn **Public Bot** off, so only you can add it to servers.
   - **Privileged Gateway Intents**: **Message Content Intent** on (required for the chat plugin) and **Server Members Intent** on (role and welcome handling).
5. **Invite the bot**: OAuth2 → URL Generator → scopes `bot` and `applications.commands`. Permissions:
   - View Channels, Send Messages, Send Messages in Threads, Create Public Threads, Read Message History, Embed Links, Attach Files, Add Reactions, Mention @everyone/@here and All Roles
   - Manage Channels, Manage Roles, Manage Webhooks, Manage Messages, Manage Threads, Manage Events, Create Events, Moderate Members
   - Manage Server (needed for AutoMod rules, Onboarding and invites during setup; you can remove it afterwards)

   Open the generated URL and pick your server. Then in Server Settings → Roles, drag the bot's role directly **below your own top role** and above every role it should assign.
6. **Store the token on your PC, not in chat.** Install the Claude Code CLI and Bun once (PowerShell):

   ```powershell
   irm https://claude.ai/install.ps1 | iex
   powershell -c "irm bun.sh/install.ps1 | iex"
   ```

   Then start `claude` in a terminal and run:

   ```text
   /plugin install discord@claude-plugins-official
   /discord:configure <token>
   ```

   Choose the **user** scope when asked. The token is saved in `%USERPROFILE%\.claude\channels\discord\.env`. My admin script reads the same file.
7. Tell me "bot is in". I then run part 3 (first as a dry run that lists every change, for your OK).

## 2. Talking to me through Discord

1. Start a terminal session in the project you want me to work in:

   ```powershell
   cd C:\Users\Philipp\Documents\sudoku_rouge_like   # or ...\Corebound
   claude --channels plugin:discord@claude-plugins-official
   ```

2. DM the bot. It answers with a pairing code. In the terminal run `/discord:access pair <code>`, then `/discord:access policy allowlist`, so nobody else can reach me.
3. Allow the private `#claude-ops` channel by its channel ID (Copy Channel ID; the plugin's `/discord:access` command and its `ACCESS.md` list the options). Other channels stay closed to the plugin.

Limits:

- Messages arrive only while that terminal session runs. A session in the VS Code extension does not receive them.
- Channels are a research preview, and the commands may change.
- Anyone on the allowlist can approve tool permissions in that session, so keep it to your own account.
- If I need a permission while you are away, the session waits until you answer.

## 3. What I set up (Claude, via the bot)

Once the bot is in, I add `tools/discord/` with a layout file (roles, categories, channels, permission overwrites, forum tags, pinned texts) and a script that applies it to the server and can rerun safely. Each run starts as a dry run, and you confirm before it changes anything. The script reads the token from the `.env` file and never prints it.

I then set up:

- the roles and channels from part 4, and the permission overwrites per category;
- texts for `#welcome`, `#rules` and `#faq` (you review them in `#release-drafts` first);
- forum tags and post guidelines for bug reports and feedback;
- an AutoMod rule set (part 5) that reports to `#mod-log`;
- webhooks for the news channels;
- the Onboarding questions (part 4.3).

**You** then enable **Community** (Server Settings → Enable Community; pick `#rules` and `#mod-log` when asked). Community is needed for announcement channels, Onboarding and the rules screen.

## 4. Server layout (proposal)

### 4.1 Roles (top to bottom)

| Role | Who | Purpose / key permissions |
|---|---|---|
| (Owner) | you | Server owner, all permissions |
| Admin | you, later trusted people | Administrator |
| Kratossos Bot | the bot (managed role) | The list in step 1.5 |
| Moderator | community moderators later | Manage Messages, Manage Threads, Moderate Members, View Audit Log |
| Playtester | beta testers | Sees the Playtest category (beta branch info) |
| Translator | French/Spanish/Portuguese helpers | Sees the Translation category |
| Run of the Nine | chosen in Onboarding | Run of the Nine channels, game pings |
| Corebound | chosen in Onboarding | Corebound channels, game pings |
| Patch Notes | opt-in | Pinged for new builds of the games they picked |
| Events | opt-in | Pinged for events and challenges |

### 4.2 Categories and channels

| Category | Channel | Type | Who can post |
|---|---|---|---|
| **Info** | `#welcome` | text, read-only | staff |
| | `#rules` | text, read-only | staff |
| | `#announcements` | announcement (studio-wide) | staff, bot |
| | `#faq` | text, read-only | staff |
| **Community** | `#general` | text | everyone |
| | `#screenshots-and-clips` | text | everyone |
| **Run of the Nine** | `#rotn-news` | announcement (patch notes) | staff, bot |
| | `#rotn-chat` | text | everyone |
| | `#rotn-seeds-and-walks` | text (share seeds, Daily/Monthly Walk) | everyone |
| | `#rotn-bugs` | forum, tags: Bug, Crash, Save, UI, Translation, Needs info, Fixed | everyone |
| | `#rotn-feedback` | forum, tags: Idea, Balance, UX, Planned, Done | everyone |
| **Corebound** | `#cb-news` | announcement | staff, bot |
| | `#cb-chat` | text | everyone |
| | `#cb-builds` | text (decks, loadouts, strategies) | everyone |
| | `#cb-bugs` | forum, same tags | everyone |
| | `#cb-feedback` | forum, same tags | everyone |
| **Playtest** (Playtester) | `#playtest-info` | text, read-only (beta branch name and password) | staff |
| | `#playtest-feedback` | forum | Playtester |
| **Translation** (Translator) | `#translation` | forum, tags: fr, es, pt-BR, de | Translator |
| **Staff** (Admin, Moderator, bot) | `#mod-log` | text (AutoMod alerts, audit) | bot |
| | `#release-drafts` | text (drafts for your OK) | staff, bot |
| | `#claude-ops` | text (you ↔ Claude) | you, bot |
| Voice (optional) | `Lounge` | voice | everyone |

The game categories are visible to everyone by default. Onboarding pre-selects what each player picked, and the rest can be hidden in the channel list.

### 4.3 Onboarding questions

1. "Which games do you play?" Run of the Nine / Corebound (multi-select): game roles and categories.
2. "Ping me for…" Patch notes / Events (multi-select): notification roles.
3. "Want to help?" Playtesting / Translating (fr, es, pt-BR): this only flags interest; you grant Playtester and Translator yourself.

## 5. Safety settings (You, or me after Community is on)

- Verification level **Medium** (verified email, account older than 5 minutes).
- Explicit media content filter: **all members**.
- Default notifications: **only @mentions**.
- **Require 2FA for moderator actions** on (this is why step 1.1 matters).
- AutoMod:
  - block mention spam (5 or more mentions);
  - block suspected spam content;
  - Discord's "Commonly flagged words" preset;
  - a custom list (invite links from other servers, scam phrases);
  - all alerts go to `#mod-log`.
- Never post the bot token, beta passwords or keys in public channels; beta info lives in `#playtest-info` only.

## 6. Release notes flow

1. For each Steam build I write the notes as now (`steam_assets/ReleaseNotes_v*.md` for Steam) plus a Discord version: Discord Markdown in an embed (a title, a short highlights list, and a link to the full Steam announcement). A bot message is limited to 2,000 characters and an embed description to 4,096, so the full list stays on Steam.
2. I post the draft in `#release-drafts`. You approve with ✅ or by replying "post".
3. I post it in `#rotn-news` or `#cb-news`, ping the game role together with Patch Notes, and press **Publish**, so servers that follow the channel get it too.
4. The post goes out when the build is live on the `default` branch and the Steam announcement is published, never before.

**Automatic mirror (since 2026-10-15).** `tools/discord/steam_news.py` reads each game's public Steam news (community announcements only) and posts every new announcement to `#rotn-news` / `#cb-news` as an embed with a link to the full notes, pinging **Patch Notes**; in an announcement channel it also presses Publish. Only announcements already published on Steam are in that feed, so nothing goes out early; the hand-written Discord version in steps 1–3 is now optional. It runs every 30 minutes through Windows Task Scheduler ("Kratossos Steam News to Discord"), so only while your PC is on and you are logged on; a run after a pause catches up. Corebound is skipped until its Steam news is public (Steam answers 403 before release). Log: `%USERPROFILE%\.claude\channels\discord\steam_news.log`. Commands: `check`, `preview <game>` (to `#release-drafts`), `publish-latest <game>`, `remove-task`.

## 7. Linking the server

- Create one permanent invite (Invite People → Edit invite link → Expire after: Never) and send it to me.
- Add it to both Steam store pages (Steamworks → Store Page Admin; the store's social link fields), to the release notes footer and to the in-game credits or main menu later.
- A vanity URL (`discord.gg/<name>`) needs server boost level 3, so the permanent invite is enough for now.

## 8. Checklist

- [ ] 1.1 2FA on · [x] 1.2 server "Kratossos" created · [x] 1.3 server ID sent to me
- [x] 1.4 bot created · [x] 1.5 bot invited (with a "Bot-Admin" role that has **Administrator**; see 9.)
- [x] 1.6 token stored · [x] 1.7 "bot is in" sent
- [ ] 2 paired, allowlist on, `#claude-ops` allowed
- [x] 3 layout applied 2026-10-14 (`tools/discord/discord_admin.py apply`): roles, categories, channels, permissions, AutoMod, role order · [ ] Community enabled · [ ] forums created (after Community) · [ ] welcome/rules/faq texts approved (drafts in `#release-drafts`)
- [x] 5 verification Medium, media filter all members, notifications @mentions only, AutoMod rules · [ ] Require 2FA for moderators (needs 1.1) · [ ] 7 permanent invite on both store pages

## 9. State on 2026-10-14

- Applied: 8 categories, 17 channels, 7 roles ordered Bot-Admin > Moderator > Playtester > Translator > Run of the Nine > Corebound > Patch Notes > Events, @everyone without @everyone-mentions, private threads and events, 4 AutoMod rules alerting to `#mod-log`. `discord_admin.py plan` reports nothing left to change.
- Waiting for **Community** (you: Server Settings → Enable Community, pick `#rules` and `#mod-log`): the six forum channels (`#rotn-bugs`, `#rotn-feedback`, `#cb-bugs`, `#cb-feedback`, `#playtest-feedback`, `#translation`), the conversion of `#announcements`, `#rotn-news` and `#cb-news` into announcement channels, and Onboarding. Then tell me, and I rerun `apply`.
- Steam news mirror installed (section 6). Run of the Nine's v0.1.5 announcement was already published, so it was recorded, not posted; `python tools/discord/steam_news.py publish-latest rotn` posts it. Corebound FAQ draft in `#release-drafts`.
- The bot role has **Administrator**. It works; a leaked token would then give full control of the server. After setup, consider replacing it with the permission list in step 1.5, and reset the token (Developer Portal → Bot → Reset Token, then `/discord:configure` in a CLI session).

## 10. State on 2026-10-15

- **Community** is on (owner). Applied: `#announcements`, `#rotn-news`, `#cb-news` are announcement channels; the six forums exist; server description; welcome screen (rules, both game chats, FAQ, general).
- **Picking roles with emojis:** Discord's own **Onboarding**, which members also reach any time under **Channels & Roles** at the top of the channel list. No extra bot needs to run, so it works around the clock. Questions (`tools/discord/layout.json`, applied by `community.py`):
  1. *Which games do you play?* — custom emojis `:run_of_the_nine:` and `:corebound:` (the games' icons); gives the game role and shows its channels.
  2. *Ping me for…* — 📰 Patch notes, 🎉 Events.
  3. *Where are you from?* — dropdown with 50 country flags; each gives a role named like "🇩🇪 Germany", so the flag shows on the member's profile. Missing countries: add them to `countries` in the layout and rerun `apply`.
  4. *Want to help?* — 🧪 Playtesting, 🌐 Translating; gives "Playtest volunteer" / "Translation volunteer", so Moderators see who offered (they still grant Playtester / Translator).
- Classic reaction roles (react to a message, get a role) would need a bot that is online all the time (hosted, or a third-party bot such as Carl-bot); Onboarding does the same without one.
- `#faq` has a Run of the Nine and a Corebound part (`texts/faq.md`, split at `---`); `discord_admin.py replace <channel> <file>` republishes a text.

## 11. Ideas for later

| Idea | What it takes |
|---|---|
| **Daily Walk of the day**: a post each morning in `#rotn-seeds-and-walks` with today's walk and a thread for times and results | a scheduled script that computes the day's walk like the game does |
| **Seed of the week** and the Monthly Walk as Discord **Events**, with a "Seed Champion" role for the winner | event posts by the bot; you judge or the game's score decides |
| **Soundboard and stickers**: the game's 8-bit effects as server sounds (8 free slots), item and relic icons as emojis (50 slots), a few stickers | upload once the 8-bit set is final |
| **Server Guide** (Community): pinned pages for "How to play", "Known issues", "Roadmap" | text only |
| **Polls** for feature votes and **dev logs** with work-in-progress screenshots | your posts; polls are built into Discord |
| **Credits for helpers**: playtesters and translators named in the game's credits | a list in the credits panel |
| **Links**: the permanent invite on both Steam store pages and in the main menu or credits | invite link + store page edit |
| **Safety**: "Require 2FA for moderator actions" and a second human Moderator before the server grows | your settings |

## 12. Daily, weekly and monthly posts (since 2026-10-15)

`tools/discord/walk_posts.py`, run every 30 minutes by the Windows task "Kratossos Walk Posts" (while your PC is on; a missed day is caught up at the next run):

- **Every day:** the Daily Walk's three goals in `#rotn-seeds-and-walks`, with a results thread.
- **Every Monday:** the **Seed of the week** (players type it into the start-run seed field; from the next update a "Week N seed" button fills it in), a Discord **Event** for the week, an @Events ping and a results thread.
- **Every 1st:** the **Monthly Walk** board (theme, size, regions, modifiers), an Event for the month, @Events ping, results thread.
- The contents come from `walk_calendar.py`, which computes them exactly like the game (GameDesignSpec RUN-WEEK-002).
- **Seed Champion:** pick the best result from a thread and run `python tools/discord/walk_posts.py champion <name>`; `champion --reset` clears the role (needs the Server Members Intent, see below).

## 13. Server extras (2026-10-15)

- **Server icon** "Kratossos Games": `tools/discord/brand/server_icon.html` (a brush ensō around the Nine with a glowing core), rendered to `server_icon.png` and set with `discord_admin.py set-icon`.
- **Emojis:** the two game icons, 8 classes, 5 bosses, 10 items and relics (`build_assets.py` makes them from the game's art). **Soundboard:** 8 of the code-made 8-bit effects (puzzle solved, level up, gold, victory, wrong digit, achievement, boss gate, fanfare).
- **Guide channels:** `#how-to-play`, `#known-issues`, `#roadmap` (texts in `texts/`, drafts in `#release-drafts` until you say "post guide"). To show them in Discord's **Server Guide**: Server Settings → Onboarding → Server Guide → Resource pages → add the three channels (the Server Guide has no public API).
- **`#dev-log`** (announcement channel, staff posts) and **`#polls`** (members vote, staff ask): `discord_admin.py poll polls "Question?" "🎵 Answer" "Answer"`.
- **Permanent invite:** https://discord.gg/5YxKpA6f6k (`discord_admin.py invite`). It is in the game's credits; for the Steam store pages: Steamworks → (app) → Store Page Admin → the social-media links (Discord) → paste, Save, then Publish (both apps; your step).
- **Community credits:** `discord_admin.py export-credits` writes the Playtester and Translator names into the game's credits (CREDITS-COMMUNITY-001). Ask helpers before giving them the role that their server name goes into the credits.

**Your steps:**
1. **Server Members Intent:** Developer Portal → your bot → Bot → Privileged Gateway Intents → *Server Members Intent* on → Save. Needed for `export-credits` and `champion --reset`.
2. **Require 2FA for moderator actions:** only the server owner can switch it on (Server Settings → Safety Setup → Permissions → *Require 2FA for moderator actions*), and your own account needs 2FA first.
3. **A second human Moderator:** pick someone you trust from the community and give them the Moderator role.
4. The Discord link on both Steam store pages (above).
