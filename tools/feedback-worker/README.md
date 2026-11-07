# Feedback relay (Cloudflare Worker)

The game's feedback form (F8, or Options › Run › Send feedback) sends its report here; this Worker forwards it to a
private Discord channel. The Discord webhook is a secret of the Worker and never part of the game. Until the Worker's
address is in `FeedbackSender.Endpoint` (Assets/Scripts/Platform/FeedbackSender.cs), the game saves every report as a
ZIP in its data folder instead (`%USERPROFILE%\AppData\LocalLow\Kratossos\sudoku_rouge_like\Feedback`).

Requirement: FEEDBACK-002 (docs/GameDesignSpec.md, "Feedback from inside the game").

## Set up (owner, once, about 15 minutes)

1. Discord: make a private channel `#player-feedback` (Staff only). Channel settings › Integrations › Webhooks › New
   Webhook, name it "Feedback", copy its URL. Keep the URL to yourself: whoever has it can post to the channel.
2. Cloudflare: a free account is enough. Install Node.js, then in this folder:
   ```
   npx wrangler login
   npx wrangler kv namespace create FEEDBACK_LIMITS
   ```
   Paste the `id` it prints into `wrangler.toml`.
3. Store the webhook as a secret (it asks for the URL; it is not written anywhere in the repo):
   ```
   npx wrangler secret put DISCORD_WEBHOOK
   npx wrangler deploy
   ```
   It prints the Worker's address, like `https://rotn-feedback.<your-name>.workers.dev`.
4. Tell me the address. I put it into `FeedbackSender.Endpoint`, and the next build sends reports straight to Discord.
   With your own domain on Cloudflare the address can be `https://feedback.<your-domain>` instead.

## What the Worker does

- Takes only multipart POSTs up to 2 MB, from an installation ID of 32 hex digits.
- Counts reports per installation and hour in the KV namespace; the sixth in an hour gets 429.
- Posts one Discord message per report: the title line ("[Bug] v0.4.3 · NumberFreak · Floor 2 · Seed 482913") and the
  files the player ticked (report.txt, screenshot.jpg, log.txt, save.json). Mentions are switched off.

## Test

`curl -F install=0123456789abcdef0123456789abcdef -F kind=Bug -F title=test -F report=@README.md <worker address>`
should post a message with this README attached.
