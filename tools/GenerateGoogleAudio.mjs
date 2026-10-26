/** Generates reusable Google Cloud TTS question assets. Default mode is an offline, non-billable plan. */
import { readFile, writeFile, mkdir, rename, access } from 'node:fs/promises';
import { resolve, dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash, createSign } from 'node:crypto';
import { execFileSync } from 'node:child_process';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const output = join(root, 'src/ToeflExam.Api/Content/Google');
const male = process.env.GOOGLE_TTS_MALE_VOICE || 'en-US-Neural2-D';
const female = process.env.GOOGLE_TTS_FEMALE_VOICE || 'en-US-Neural2-F';
const project = process.env.GOOGLE_CLOUD_PROJECT;
const rate = 16000;

/** Reads only authored question material; no student answer or recording is sent to Google. */
export async function inventory() {
    const rows = [];
    for (const file of ['practice-v1.json', 'exams-v1.json']) {
        const entries = JSON.parse(await readFile(join(root, 'src/ToeflExam.Infrastructure/ContentBank', file), 'utf8'));
        for (const e of entries.filter(e => ['speaking', 'listening'].includes(e.section))) {
            if (!/^[a-f0-9]{32}\.wav$/.test(e.audioFile)) throw new Error('Invalid content asset name.');
            rows.push({ collection: e.section === 'speaking' && e.pool === 'practice' ? 'Speaking' : 'Bank', name: e.audioFile, text: e.section === 'speaking' ? e.prompt : e.text });
        }
    }
    const source = await readFile(join(root, 'src/ToeflExam.Infrastructure/Persistence/SpeakingSeed.cs'), 'utf8');
    const groups = [...source.matchAll(/\["(?:Welcome|Think)[\s\S]*?\]/g)];
    if (groups.length !== 2) throw new Error('Legacy Speaking inventory changed; review the source parser.');
    groups.forEach((g, i) => [...g[0].matchAll(/"([^"]+)"/g)].forEach((m, j) => rows.push({ collection: 'Speaking', name: `${i + 1}-${j + 1}-v1.wav`, text: m[1] })));
    if (new Set(rows.map(r => `${r.collection}/${r.name}`)).size !== rows.length) throw new Error('Duplicate audio asset.');
    return rows;
}

/** Preserves dialogue turns and maps A/B to consistent male/female neural voices. */
export function turns(text) {
    return text.split(/\r?\n/).filter(t => t.trim()).map(line => ({
        voice: /^B:/.test(line) ? female : male,
        text: line.replace(/^[AB]:\s*/, '').trim(),
    }));
}

/** Rejects incompatible WAV data before concatenating synthesized segments. */
export function pcm(wav) {
    if (wav.toString('ascii', 0, 4) !== 'RIFF' || wav.toString('ascii', 8, 12) !== 'WAVE') throw new Error('Google returned invalid WAV audio.');
    let format = false;
    for (let offset = 12; offset + 8 <= wav.length;) {
        const length = wav.readUInt32LE(offset + 4), start = offset + 8;
        if (start + length > wav.length) throw new Error('Truncated WAV audio.');
        const kind = wav.toString('ascii', offset, offset + 4);
        if (kind === 'fmt ') {
            if (length < 16 || wav.readUInt16LE(start) !== 1 || wav.readUInt16LE(start + 2) !== 1 || wav.readUInt32LE(start + 4) !== rate || wav.readUInt16LE(start + 14) !== 16) throw new Error('Expected PCM16 mono 16 kHz.');
            format = true;
        }
        if (kind === 'data') {
            if (!format || length < 2 || length % 2) throw new Error('Invalid PCM samples.');
            return wav.subarray(start, start + length);
        }
        offset = start + length + (length % 2);
    }
    throw new Error('No PCM samples.');
}

/** Writes a standard PCM WAV header, keeping exam duration calculations compatible. */
export function wave(samples) {
    const h = Buffer.alloc(44);
    h.write('RIFF'); h.writeUInt32LE(samples.length + 36, 4); h.write('WAVEfmt ', 8);
    h.writeUInt32LE(16, 16); h.writeUInt16LE(1, 20); h.writeUInt16LE(1, 22);
    h.writeUInt32LE(rate, 24); h.writeUInt32LE(rate * 2, 28); h.writeUInt16LE(2, 32); h.writeUInt16LE(16, 34);
    h.write('data', 36); h.writeUInt32LE(samples.length, 40);
    return Buffer.concat([h, samples]);
}

let token, expires = 0;
/** Uses a short-lived access token from a service account or locally authorized Google Cloud CLI. */
async function accessToken() {
    if (token && Date.now() < expires) return token;
    if (process.env.GOOGLE_APPLICATION_CREDENTIALS) {
        const key = JSON.parse(await readFile(process.env.GOOGLE_APPLICATION_CREDENTIALS, 'utf8'));
        if (key.type !== 'service_account' || !key.private_key || !key.client_email) throw new Error('Use a service-account credential file, or gcloud application-default login.');
        const now = Math.floor(Date.now() / 1000);
        const encode = o => Buffer.from(JSON.stringify(o)).toString('base64url');
        const unsigned = encode({ alg: 'RS256', typ: 'JWT' }) + '.' + encode({ iss: key.client_email, scope: 'https://www.googleapis.com/auth/cloud-platform', aud: 'https://oauth2.googleapis.com/token', iat: now, exp: now + 3600 });
        const assertion = unsigned + '.' + createSign('RSA-SHA256').update(unsigned).sign(key.private_key, 'base64url');
        const response = await fetch('https://oauth2.googleapis.com/token', { method: 'POST', body: new URLSearchParams({ grant_type: 'urn:ietf:params:oauth:grant-type:jwt-bearer', assertion }), signal: AbortSignal.timeout(30000) });
        if (!response.ok) throw new Error(`Google authentication failed (HTTP ${response.status}). Credential values are not logged.`);
        token = (await response.json()).access_token;
    } else {
        try { token = execFileSync(process.platform === 'win32' ? 'cmd.exe' : 'gcloud', process.platform === 'win32' ? ['/d', '/s', '/c', 'gcloud auth application-default print-access-token'] : ['auth', 'application-default', 'print-access-token'], { encoding: 'utf8', windowsHide: true, stdio: ['ignore', 'pipe', 'pipe'], timeout: 30000 }).trim(); }
        catch { throw new Error('Google Cloud credentials are unavailable. Configure GOOGLE_APPLICATION_CREDENTIALS or gcloud application-default login.'); }
    }
    if (!token) throw new Error('Google did not return an access token.');
    expires = Date.now() + 45 * 60 * 1000;
    return token;
}

/** Synthesizes a single bounded turn; errors expose only status, never credentials or response bodies. */
async function synthesize(turn) {
    if (Buffer.byteLength(turn.text, 'utf8') > 5000) throw new Error('A turn exceeds the Cloud TTS input limit; split the authored material.');
    const response = await fetch('https://texttospeech.googleapis.com/v1/text:synthesize', {
        method: 'POST', headers: { Authorization: `Bearer ${await accessToken()}`, 'Content-Type': 'application/json', ...(project ? { 'x-goog-user-project': project } : {}) },
        body: JSON.stringify({ input: { text: turn.text }, voice: { languageCode: 'en-US', name: turn.voice }, audioConfig: { audioEncoding: 'LINEAR16', sampleRateHertz: rate, speakingRate: 0.95 } }),
        signal: AbortSignal.timeout(60000),
    });
    if (!response.ok) throw new Error(`Cloud TTS synthesis failed (HTTP ${response.status}); check API enablement, billing, permissions and quota. Existing audio was preserved.`);
    const data = await response.json();
    if (typeof data.audioContent !== 'string') throw new Error('Cloud TTS returned no audio.');
    return pcm(Buffer.from(data.audioContent, 'base64'));
}

/** Builds missing assets into an isolated directory and publishes each complete WAV atomically. */
async function main() {
    const rows = await inventory();
    const count = rows.reduce((n, r) => n + turns(r.text).length, 0);
    console.log(`Audio plan: ${rows.length} assets, ${count} speech turns, ${rows.reduce((n,r) => n+r.text.length,0)} text characters. Voices: ${male}, ${female}.`);
    if (!process.argv.includes('--generate')) { console.log('No network calls made. Use --generate after configuring Google Cloud TTS. See docs/GOOGLE-TTS.md.'); return; }
    await accessToken();
    const limitArg = process.argv.find(a => a.startsWith('--limit='));
    const limit = limitArg ? Number(limitArg.split('=')[1]) : rows.length;
    if (!Number.isInteger(limit) || limit < 1) throw new Error('Invalid --limit.');
    let generated = 0;
    for (const row of rows) {
        const folder = join(output, row.collection), file = join(folder, row.name);
        try { await access(file); continue; } catch { /* Only missing assets are generated; old editions are never overwritten. */ }
        const pieces = [];
        for (const turn of turns(row.text)) { pieces.push(await synthesize(turn)); pieces.push(Buffer.alloc(rate / 2)); }
        const bytes = wave(Buffer.concat(pieces));
        await mkdir(folder, { recursive: true });
        const temporary = file + '.tmp';
        await writeFile(temporary, bytes); await rename(temporary, file);
        await writeFile(file + '.provenance.json', JSON.stringify({ provider: 'Google Cloud Text-to-Speech', male, female, sha256: createHash('sha256').update(bytes).digest('hex'), textSha256: createHash('sha256').update(row.text).digest('hex'), generatedAt: new Date().toISOString() }, null, 2));
        console.log(`Created ${row.collection}/${row.name}`);
        if (++generated >= limit) break;
    }
    console.log(`Generated ${generated} complete recordings. Existing authored audio remains unchanged.`);
}
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) main().catch(error => { console.error(error.message); process.exitCode = 1; });
