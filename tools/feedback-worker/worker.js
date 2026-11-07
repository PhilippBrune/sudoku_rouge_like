// [REQ: FEEDBACK-002] Run of the Nine feedback relay: the game posts a report here, the Worker forwards it to a private
// Discord channel. The webhook lives only in this Worker's secret DISCORD_WEBHOOK, never in the game.
// Limits: 2 MB per report (counted while reading, not trusted from the header), 5 reports an hour per installation ID and
// 20 an hour per address (KV namespace FEEDBACK_LIMITS; read-then-write, so the limit is approximate), only multipart POST.

const MAX_BYTES = 2 * 1024 * 1024;
const MAX_TEXT_PART = 1024 * 1024;
const PER_HOUR_INSTALL = 5;
const PER_HOUR_ADDRESS = 20;
const KINDS = new Set(['Bug', 'Idea', 'Balance', 'Text']);
const FILES = { report: 'report.txt', screenshot: 'screenshot.jpg', log: 'log.txt', save: 'save.json' };

// Reads the body and stops above MAX_BYTES, whatever Content-Length says.
async function readCapped(request) {
  if (!request.body) return null;
  const reader = request.body.getReader();
  const chunks = [];
  let total = 0;
  for (;;) {
    const { done, value } = await reader.read();
    if (done) break;
    total += value.byteLength;
    if (total > MAX_BYTES) { await reader.cancel(); return null; }
    chunks.push(value);
  }
  const body = new Uint8Array(total);
  let at = 0;
  for (const chunk of chunks) { body.set(chunk, at); at += chunk.byteLength; }
  return body;
}

// Counts one report for this key and hour; false when the hour is full. Checked before anything is written.
async function underLimit(env, key, limit) {
  const slot = `${key}:${Math.floor(Date.now() / 3600000)}`;
  const count = Number((await env.FEEDBACK_LIMITS.get(slot)) || 0);
  if (count >= limit) return false;
  await env.FEEDBACK_LIMITS.put(slot, String(count + 1), { expirationTtl: 7200 });
  return true;
}

// Plain words only: no new lines, links, markdown or mentions in the Discord message.
const cleanTitle = (title, kind) => (String(title || `[${kind}]`).replace(/[\r\n]+/g, ' ').replace(/[^\p{L}\p{N} .,:·\-\[\]]/gu, '')
  .slice(0, 180) || `[${kind}]`);

async function isJpeg(file) {
  const head = new Uint8Array(await file.slice(0, 3).arrayBuffer());
  return head[0] === 0xFF && head[1] === 0xD8 && head[2] === 0xFF;
}

export default {
  async fetch(request, env) {
    if (request.method !== 'POST') return new Response('POST only', { status: 405 });
    if (!env.DISCORD_WEBHOOK) return new Response('not set up', { status: 503 });
    const type = request.headers.get('content-type') || '';
    if (!type.startsWith('multipart/form-data')) return new Response('multipart only', { status: 415 });

    const address = request.headers.get('CF-Connecting-IP') || 'unknown';
    if (!(await underLimit(env, `ip:${address}`, PER_HOUR_ADDRESS))) return new Response('too many', { status: 429 });

    const body = await readCapped(request);
    if (!body) return new Response('too large', { status: 413 });
    let form;
    try { form = await new Response(body, { headers: { 'content-type': type } }).formData(); }
    catch { return new Response('bad form', { status: 400 }); }

    const install = String(form.get('install') || '');
    if (!/^[0-9a-f]{32}$/.test(install)) return new Response('bad install id', { status: 400 });
    if (!(await underLimit(env, install, PER_HOUR_INSTALL))) return new Response('too many', { status: 429 });
    const kind = KINDS.has(String(form.get('kind'))) ? String(form.get('kind')) : 'Bug';

    const out = new FormData();
    out.append('payload_json', JSON.stringify({
      content: `**${cleanTitle(form.get('title'), kind)}**\nfrom installation ${install.slice(0, 8)}`,
      allowed_mentions: { parse: [] },
    }));
    let index = 0;
    for (const [field, name] of Object.entries(FILES)) {
      const file = form.get(field);
      if (!file || typeof file !== 'object' || file.size === 0) continue;
      if (field === 'screenshot' ? !(await isJpeg(file)) : file.size > MAX_TEXT_PART) continue;   // skip what is not what it says
      out.append(`files[${index++}]`, file, name);
    }

    try {
      const sent = await fetch(env.DISCORD_WEBHOOK, { method: 'POST', body: out });
      return new Response(sent.ok ? 'ok' : 'relay failed', { status: sent.ok ? 200 : 502 });
    } catch {
      return new Response('relay failed', { status: 502 });
    }
  },
};
