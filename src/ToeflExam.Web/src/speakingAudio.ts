// Capture PCM directly. No compressed recording or decodeAudioData dependency.
export interface PcmRecording {
    start(): Promise<Blob>;
    stop(): void;
    cancel(): void;
}
/** Captures bounded PCM microphone samples without decoding a compressed recording. */
export async function preparePcmRecording(
    stream: MediaStream,
    seconds: number,
): Promise<PcmRecording> {
    if (!Number.isInteger(seconds) || seconds < 1 || seconds > 45)
        throw new Error("Invalid recording duration.");
    const context = new AudioContext();
    let source: MediaStreamAudioSourceNode | undefined,
        node: AudioWorkletNode | undefined;
    let settled = false,
        started = false,
        samples = 0;
    let stopTimer: ReturnType<typeof setTimeout> | undefined;
    const chunks: Float32Array[] = [];
    let resolve!: (wave: Blob) => void, reject!: (error: Error) => void;
    const result = new Promise<Blob>((yes, no) => {
        resolve = yes;
        reject = no;
    });
    void result.catch(() => {}); // Cancellation can precede start while the question plays.
    /** Releases microphone tracks, AudioWorklet ports and the audio context. */
    const cleanup = () => {
        clearTimeout(stopTimer);
        source?.disconnect();
        node?.disconnect();
        node?.port.close();
        stream.getTracks().forEach((track) => {
            track.removeEventListener("ended", disconnected);
            track.stop();
        });
        context.onstatechange = null;
        void context.close().catch(() => {});
    };
    /** Rejects the current recording once and clears its retained audio samples. */
    const fail = (error: Error) => {
        if (settled) return;
        settled = true;
        chunks.length = 0;
        cleanup();
        reject(error);
    };
    /** Converts microphone disconnection into a recoverable recording failure. */
    const disconnected = () =>
        fail(
            new Error(
                "The microphone was disconnected. Reconnect it and try again.",
            ),
        );
    try {
        if (!context.audioWorklet)
            throw new Error(
                "Recording requires HTTPS and a browser with AudioWorklet support.",
            );
        await context.resume();
        await context.audioWorklet.addModule("/audio/pcm-recorder.js");
        node = new AudioWorkletNode(context, "toefl-pcm-recorder", {
            processorOptions: {
                maxSamples: Math.ceil(context.sampleRate * seconds),
            },
        });
        node.onprocessorerror = () =>
            fail(new Error("Recording was interrupted. Please try again."));
        node.port.onmessage = async ({ data }) => {
            if (settled) return;
            if (data.samples instanceof Float32Array) {
                samples += data.samples.length;
                if (samples > Math.ceil(context.sampleRate * seconds)) {
                    fail(new Error("Recording exceeded the allowed duration."));
                    return;
                }
                chunks.push(data.samples);
            }
            if (data.done) {
                settled = true;
                cleanup();
                try {
                    const pcm = new Float32Array(samples);
                    let offset = 0;
                    for (const chunk of chunks) {
                        pcm.set(chunk, offset);
                        offset += chunk.length;
                    }
                    chunks.length = 0;
                    resolve(await pcmToWave(pcm, context.sampleRate, seconds));
                } catch {
                    reject(
                        new Error(
                            "No usable audio was recorded. Check your microphone and try again.",
                        ),
                    );
                }
            }
        };
        source = context.createMediaStreamSource(stream);
        source.connect(node);
        node.connect(context.destination); // Worklet output is silent; no microphone feedback.
        stream
            .getTracks()
            .forEach((track) => track.addEventListener("ended", disconnected));
        context.onstatechange = () => {
            if (started && context.state !== "running" && !settled)
                fail(
                    new Error(
                        "Recording was paused by the browser. Please try again.",
                    ),
                );
        };
        return {
            /** Starts bounded PCM capture once and schedules automatic stop. */
            start() {
                if (!started && !settled) {
                    started = true;
                    node!.port.postMessage("start");
                }
                return result;
            },
            /** Asks the audio worklet to finish and return its captured samples. */
            stop() {
                if (!started || settled || stopTimer) return;
                node!.port.postMessage("stop");
                stopTimer = setTimeout(
                    () =>
                        fail(
                            new Error(
                                "The microphone did not finish recording. Please try again.",
                            ),
                        ),
                    3000,
                );
            },
            /** Discards the pending recording and releases its microphone resources. */
            cancel() {
                fail(new DOMException("Recording cancelled.", "AbortError"));
            },
        };
    } catch (error) {
        fail(
            error instanceof Error
                ? error
                : new Error("The microphone could not be initialized."),
        );
        throw error;
    }
}
/** Encodes validated mono PCM samples as a bounded WAV recording. */
export async function pcmToWave(
    pcm: Float32Array,
    sampleRate: number,
    seconds: number,
): Promise<Blob> {
    const length = Math.min(
        Math.floor((pcm.length * 16000) / sampleRate),
        seconds * 16000,
    );
    if (length < 1600) throw new Error("Recording is too short.");
    const offline = new OfflineAudioContext(1, length, 16000);
    const buffer = offline.createBuffer(1, pcm.length, sampleRate);
    buffer.getChannelData(0).set(pcm);
    const source = offline.createBufferSource();
    source.buffer = buffer;
    source.connect(offline.destination);
    source.start();
    const samples = (await offline.startRendering()).getChannelData(0);
    const data = new ArrayBuffer(44 + length * 2),
        view = new DataView(data);
    /** Writes an ASCII WAV header field at the specified byte offset. */
    const text = (offset: number, value: string) =>
        [...value].forEach((c, i) =>
            view.setUint8(offset + i, c.charCodeAt(0)),
        );
    text(0, "RIFF");
    view.setUint32(4, data.byteLength - 8, true);
    text(8, "WAVE");
    text(12, "fmt ");
    view.setUint32(16, 16, true);
    view.setUint16(20, 1, true);
    view.setUint16(22, 1, true);
    view.setUint32(24, 16000, true);
    view.setUint32(28, 32000, true);
    view.setUint16(32, 2, true);
    view.setUint16(34, 16, true);
    text(36, "data");
    view.setUint32(40, length * 2, true);
    for (let i = 0; i < length; i++) {
        const s = Math.max(-1, Math.min(1, samples[i]));
        view.setInt16(
            44 + i * 2,
            Math.round(s * (s < 0 ? 32768 : 32767)),
            true,
        );
    }
    return new Blob([data], { type: "audio/wav" });
}
/** Encodes a validated WAV blob for the bounded assessment request. */
export async function waveBase64(blob: Blob) {
    const bytes = new Uint8Array(await blob.arrayBuffer());
    let binary = "";
    for (let offset = 0; offset < bytes.length; offset += 8192)
        binary += String.fromCharCode(...bytes.subarray(offset, offset + 8192));
    return btoa(binary);
}
