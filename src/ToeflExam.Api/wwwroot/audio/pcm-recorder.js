class PcmRecorder extends AudioWorkletProcessor {
    constructor({ processorOptions }) {
        super();
        this.limit = processorOptions.maxSamples;
        this.total = 0;
        this.used = 0;
        this.buffer = new Float32Array(2048);
        this.active = false;
        this.done = false;
        this.port.onmessage = ({ data }) => {
            if (data === "start" && !this.done) this.active = true;
            if (data === "stop") this.finish();
        };
    }
    flush() {
        if (!this.used) return;
        const samples = this.buffer.slice(0, this.used);
        this.port.postMessage({ samples }, [samples.buffer]);
        this.used = 0;
    }
    finish() {
        if (this.done) return;
        this.done = true;
        this.active = false;
        this.flush();
        this.port.postMessage({ done: true });
    }
    process(inputs) {
        if (this.done) return false;
        const channels = inputs[0];
        if (!this.active || !channels?.length) return true;
        for (
            let i = 0;
            i < channels[0].length && this.total < this.limit;
            i++
        ) {
            let value = 0;
            for (const channel of channels) value += channel[i];
            this.buffer[this.used++] = value / channels.length;
            this.total++;
            if (this.used === this.buffer.length) this.flush();
        }
        if (this.total >= this.limit) this.finish();
        return !this.done;
    }
}
registerProcessor("toefl-pcm-recorder", PcmRecorder);
