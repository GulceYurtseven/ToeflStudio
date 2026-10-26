import test from 'node:test';
import assert from 'node:assert/strict';
import { inventory, turns, pcm, wave } from './GenerateGoogleAudio.mjs';

test('every authored audio asset has a unique destination and speech', async () => {
    const rows = await inventory();
    assert.equal(rows.length, 219);
    assert.equal(new Set(rows.map(r => `${r.collection}/${r.name}`)).size, rows.length);
    assert.ok(rows.every(r => turns(r.text).every(t => t.text.length > 0)));
});

test('dialogue labels become distinct voices without spoken labels', () => {
    const result = turns('A: Hello there.\nB: Good morning.');
    assert.notEqual(result[0].voice, result[1].voice);
    assert.deepEqual(result.map(r => r.text), ['Hello there.', 'Good morning.']);
});

test('PCM WAV preserves samples and rejects truncated or incompatible audio', () => {
    const samples = Buffer.from([0, 0, 1, 0, 255, 127]);
    const wav = wave(samples);
    assert.deepEqual(pcm(wav), samples);
    assert.throws(() => pcm(wav.subarray(0, wav.length - 1)));
    const stereo = Buffer.from(wav);
    stereo.writeUInt16LE(2, 22);
    assert.throws(() => pcm(stereo));
});
