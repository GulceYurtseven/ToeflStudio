import { useEffect, useRef, useState } from "react";
import { ApiError, post, request } from "./api";
import {
    ConfirmDialog,
    ErrorBox,
    Icon,
    navigate,
    Progress,
    Spinner,
} from "./components";
import {
    preparePcmRecording,
    waveBase64,
    type PcmRecording,
} from "./speakingAudio";

/** Maps microphone and assessment failures to recoverable English guidance. */
function speakingError(error: unknown): string {
    if (error instanceof ApiError)
        return (
            (
                {
                    0: "Unable to reach the server. Keep this page open and retry with the same recording.",
                    400: "This recording could not be accepted. Record your response again.",
                    401: "Your session has expired. Please sign in again.",
                    404: "This activity is unavailable or does not belong to your account.",
                    409: "This activity has changed or is being assessed. Refresh its status before trying again.",
                    410: "Detailed feedback has expired. Your score and progress are saved.",
                    429: "Too many requests. Wait a minute before trying again.",
                    502: "The audio could not be assessed. Your progress has not changed. Check your recording and try again.",
                    503: "AI assessment is temporarily unavailable. Keep this page open to retry with your recording.",
                } as Record<number, string>
            )[error.status] ??
            "The request could not be completed. Please try again."
        );
    return error instanceof Error
        ? error.message
        : "The request could not be completed.";
}

const modules = {
    "listen-repeat": {
        title: "Listen and Repeat",
        description: "Repeat each sentence accurately and clearly.",
        count: 7,
    },
    interview: {
        title: "Take an Interview",
        description:
            "Share your experiences and opinions in a four-question interview.",
        count: 4,
    },
};
type Module = keyof typeof modules;
/** Narrows a route value to a supported Speaking task name. */
export const isSpeakingModule = (s: string): s is Module =>
    Object.hasOwn(modules, s);
/** Builds the API root for a validated task module. */
const base = (m: Module) => "/api/practice/speaking/" + m;
/** Builds the Speaking activity route for the requested run position. */
const path = (m: Module, id: string, p: number) => `/speaking/${m}/${id}/${p}`;
type Run = {
    id: string;
    module: Module;
    isCompleted: boolean;
    completedSteps: number;
    totalSteps: number;
    completionPercent: number;
    averageRawScore: number | null;
    nextPosition: number | null;
    steps: {
        position: number;
        status: string;
        score: number | null;
        submissionId: string | null;
    }[];
};
type Report = {
    title: string;
    prompt: string;
    module: string;
    position: number;
    durationMilliseconds: number;
    rubricVersion: string;
    model: string;
    expiresAt: string;
    feedback: {
        score: number;
        transcript: string;
        rationale: string;
        strengths: string[];
        issues: { category: string; explanation: string; suggestion: string }[];
        nextSteps: string[];
    };
};
type Step = {
    question: {
        title: string;
        instructions: string;
        context: string;
        responseSeconds: number;
        scenePosition: number;
    };
    report: Report | null;
};
type Overview = {
    configured: boolean;
    modules: {
        module: Module;
        title: string;
        totalSteps: number;
        completedSteps: number;
        completionPercent: number;
        runId: string | null;
    }[];
};

/** Illustrates the current Listen and Repeat scene without revealing the sentence. */
function CampusScene({
    position,
    context,
}: {
    position: number;
    context: string;
}) {
    // Original code-native illustration; neutral labels never reveal the sentence to repeat.
    return (
        <svg
            className="campus-scene"
            viewBox="0 0 560 190"
            role="img"
            aria-label={`${context}, stop ${position}`}
        >
            <rect x="5" y="5" width="550" height="180" rx="20" fill="#ece7ff" />
            <path
                d="M60 150V45h130v105m25 0V25h135v125m25 0V60h125v90"
                fill="#fff"
                stroke="#7863be"
                strokeWidth="3"
            />
            {[90, 130, 245, 285, 410, 450].map((x) => (
                <rect
                    key={x}
                    x={x}
                    y="75"
                    width="22"
                    height="28"
                    rx="3"
                    fill="#81d8ce"
                />
            ))}
            <path d="M25 151h510" stroke="#7863be" strokeWidth="4" />
            <text
                x="280"
                y="48"
                textAnchor="middle"
                fill="#4b367e"
                fontSize="12"
            >
                GUIDED VISIT
            </text>
            {Array.from({ length: 7 }, (_, i) => (
                <g key={i}>
                    <circle
                        cx={65 + i * 70}
                        cy="164"
                        r={position === i + 1 ? 15 : 10}
                        fill={position === i + 1 ? "#7048de" : "#c5b8eb"}
                    />
                    <text
                        x={65 + i * 70}
                        y="169"
                        textAnchor="middle"
                        fill={position === i + 1 ? "white" : "#4b367e"}
                        fontSize="12"
                    >
                        {i + 1}
                    </text>
                </g>
            ))}
        </svg>
    );
}

/** Coordinates prompt playback, timed recording, transcription assessment and retries. */
export function SpeakingPractice({
    module,
    runId,
    position,
}: {
    module: Module;
    runId: string;
    position: number;
}) {
    const url = `${base(module)}/runs/${runId}`;
    const stepUrl = `${url}/steps/${position}`;
    const [run, setRun] = useState<Run | null>(null);
    const [step, setStep] = useState<Step | null>(null);
    const [configured, setConfigured] = useState(false);
    const [error, setError] = useState("");
    const [busy, setBusy] = useState(false);
    const [mode, setMode] = useState<
        | "idle"
        | "permission"
        | "listening"
        | "recording"
        | "processing"
        | "ready"
    >("idle");
    const [remaining, setRemaining] = useState(0);
    const [recording, setRecording] = useState<Blob | null>(null);
    const [recordingUrl, setRecordingUrl] = useState("");
    const [attempted, setAttempted] = useState(false);
    const [confirm, setConfirm] = useState<"skip" | "restart" | null>(null);
    const alive = useRef(true);
    const generation = useRef(0);
    const input = useRef<MediaStream | null>(null);
    const recorder = useRef<PcmRecording | null>(null);
    const player = useRef<HTMLAudioElement | null>(null);
    const timer = useRef<ReturnType<typeof setInterval> | null>(null);
    const promptUrl = useRef("");
    const frozen = useRef<{
        submissionId: string;
        audioBase64: string;
        skip: boolean;
    } | null>(null);
    /** Stops active audio and releases microphone resources when leaving the activity. */
    function stopDevices() {
        if (timer.current) clearInterval(timer.current);
        timer.current = null;
        player.current?.pause();
        player.current = null;
        recorder.current?.cancel();
        recorder.current = null;
        input.current?.getTracks().forEach((t) => t.stop());
        input.current = null;
    }
    /** Ends the active recording at its response deadline. */
    function finishRecording() {
        if (timer.current) clearInterval(timer.current);
        timer.current = null;
        setMode("processing");
        recorder.current?.stop();
    }
    /** Loads the current Speaking activity and its saved progress. */
    async function load() {
        try {
            const [r, s, o] = await Promise.all([
                request<Run>(url),
                request<Step>(stepUrl),
                request<Overview>("/api/speaking"),
            ]);
            if (!alive.current) return;
            setRun(r);
            setStep(s);
            setConfigured(o.configured);
            if (
                r.steps.find((item) => item.position === position)?.score !=
                null
            ) {
                setRecording(null);
                setMode("idle");
                frozen.current = null;
            }
        } catch (e) {
            if (alive.current) setError(speakingError(e));
        }
    }
    useEffect(() => {
        alive.current = true;
        void load();
        return () => {
            alive.current = false;
            generation.current++;
            stopDevices();
            if (promptUrl.current) URL.revokeObjectURL(promptUrl.current);
        };
    }, [url, stepUrl]);
    useEffect(() => {
        if (!recording) {
            setRecordingUrl("");
            return;
        }
        const value = URL.createObjectURL(recording);
        setRecordingUrl(value);
        return () => URL.revokeObjectURL(value);
    }, [recording]);
    useEffect(() => {
        /** Warns before leaving an unfinished response that cannot be recovered automatically. */
        const warn = (e: BeforeUnloadEvent) => {
            if (recording || mode === "recording") {
                e.preventDefault();
                e.returnValue = "";
            }
        };
        window.addEventListener("beforeunload", warn);
        return () => window.removeEventListener("beforeunload", warn);
    }, [recording, mode]);
    useEffect(() => {
        /** Handles a hidden tab so background recording does not continue unexpectedly. */
        const hidden = () => {
            if (document.hidden && ["listening", "recording"].includes(mode)) {
                generation.current++;
                stopDevices();
                setMode("idle");
                setError(
                    "The page moved to the background. Recording has been cancelled and the microphone switched off. Please try again.",
                );
            }
        };
        document.addEventListener("visibilitychange", hidden);
        return () => document.removeEventListener("visibilitychange", hidden);
    }, [mode]);

    /** Prepares microphone capture and starts the current prompt-response sequence. */
    async function begin() {
        if (!step || attempted) return;
        const version = ++generation.current;
        stopDevices();
        setRecording(null);
        setError("");
        setMode("permission");
        try {
            if (
                !navigator.mediaDevices?.getUserMedia ||
                !window.AudioWorkletNode
            )
                throw new Error(
                    "Your browser does not support this recording method. Use an updated browser over HTTPS.",
                );
            const stream = await navigator.mediaDevices.getUserMedia({
                audio: { echoCancellation: true, noiseSuppression: true },
                video: false,
            });
            if (!alive.current || generation.current !== version) {
                stream.getTracks().forEach((t) => t.stop());
                return;
            }
            input.current = stream;
            const blob = await request<Blob>(
                stepUrl + "/audio",
                {},
                true,
                "blob",
            );
            if (!alive.current || generation.current !== version) return;
            if (promptUrl.current) URL.revokeObjectURL(promptUrl.current);
            promptUrl.current = URL.createObjectURL(blob);
            const audio = new Audio(promptUrl.current);
            player.current = audio;
            audio.onerror = () => {
                if (alive.current && generation.current === version) {
                    stopDevices();
                    setMode("idle");
                    setError(
                        "The question audio could not be played. Please try again.",
                    );
                }
            };
            const capture = await preparePcmRecording(
                stream,
                step.question.responseSeconds,
            );
            if (!alive.current || generation.current !== version) {
                capture.cancel();
                return;
            }
            recorder.current = capture;
            audio.onended = async () => {
                if (!alive.current || generation.current !== version) return;
                const end =
                    performance.now() + step.question.responseSeconds * 1000;
                setRemaining(step.question.responseSeconds);
                setMode("recording");
                timer.current = setInterval(() => {
                    setRemaining(
                        Math.max(
                            0,
                            Math.ceil((end - performance.now()) / 1000),
                        ),
                    );
                    if (performance.now() >= end) finishRecording();
                }, 100);
                try {
                    const wave = await capture.start();
                    if (!alive.current || generation.current !== version)
                        return;
                    if (timer.current) clearInterval(timer.current);
                    timer.current = null;
                    input.current = null;
                    recorder.current = null;
                    setRecording(wave);
                    frozen.current = null;
                    setMode("ready");
                } catch (e) {
                    if (alive.current && generation.current === version) {
                        stopDevices();
                        setMode("idle");
                        setError(speakingError(e));
                    }
                }
            };
            setMode("listening");
            await audio.play();
        } catch (e) {
            if (!alive.current || generation.current !== version) return;
            stopDevices();
            setMode("idle");
            setError(
                e instanceof DOMException && e.name === "NotAllowedError"
                    ? "Microphone or audio playback permission was denied. Check your browser permissions and try again."
                    : speakingError(e),
            );
        }
    }
    /** Freezes the current response and submits it with the same identifier on retries. */
    async function submit(skip = false) {
        if (busy || (!skip && !recording && !frozen.current)) return;
        setBusy(true);
        setError("");
        setConfirm(null);
        try {
            if (!frozen.current)
                frozen.current = {
                    submissionId: crypto.randomUUID(),
                    audioBase64: skip ? "" : await waveBase64(recording!),
                    skip,
                };
            setAttempted(true);
            const result = await post<{
                progress: Run;
                report: Report | null;
                reportUnavailable: boolean;
            }>(stepUrl + "/submissions", frozen.current);
            if (!alive.current) return;
            setRun(result.progress);
            setStep((s) => s && { ...s, report: result.report });
            setRecording(null);
            setMode("idle");
        } catch (e) {
            if (!alive.current) return;
            setError(speakingError(e));
            if (e instanceof ApiError && [400, 502, 503].includes(e.status)) {
                setAttempted(false);
                frozen.current = null;
            }
        } finally {
            if (alive.current) setBusy(false);
        }
    }
    /** Starts a fresh run after completion and navigates to its first activity. */
    async function restart() {
        setConfirm(null);
        setBusy(true);
        try {
            const r = await post<Run>(url + "/restart");
            navigate(path(module, r.id, 1));
        } catch (e) {
            setError(speakingError(e));
        } finally {
            setBusy(false);
        }
    }
    const progress = run?.steps.find((s) => s.position === position);
    const complete = progress?.score != null;
    const active = [
        "permission",
        "listening",
        "recording",
        "processing",
    ].includes(mode);
    const report = step?.report;
    return (
        <>
            <div className="practice-top no-print">
                <a className="back-link" href="#/speaking">
                    <Icon name="back" /> Back to Speaking Studio
                </a>
                <span className="mode-badge">Timed practice</span>
            </div>
            <div className="page-heading">
                <div>
                    <h1 id="page-title" tabIndex={-1}>
                        {modules[module].title}
                    </h1>
                    <p>{step?.question.context}</p>
                </div>
                <span>
                    ACTIVITY {position} / {run?.totalSteps ?? "—"}
                </span>
            </div>
            {error && (
                <ErrorBox retryLabel="Try again" message={error} retry={load} />
            )}
            {!run || !step ? (
                !error && <Spinner text="Loading…" />
            ) : (
                <>
                    <div className="speaking-steps no-print">
                        {run.steps.map((s) => (
                            <button
                                key={s.position}
                                aria-label={`Activity ${s.position}`}
                                aria-current={
                                    s.position === position ? "step" : undefined
                                }
                                disabled={
                                    active ||
                                    busy ||
                                    (!complete && !!recording) ||
                                    s.position > run.completedSteps + 1
                                }
                                onClick={() =>
                                    navigate(path(module, runId, s.position))
                                }
                            >
                                {s.status === "completed" ? (
                                    <Icon name="check" size={16} />
                                ) : (
                                    s.position
                                )}
                            </button>
                        ))}
                    </div>
                    <Progress
                        value={run.completionPercent}
                        label="Speaking run completion"
                    />
                    {!complete && (
                        <section className="speaking-recorder no-print">
                            {module === "listen-repeat" ? (
                                <CampusScene
                                    position={step.question.scenePosition}
                                    context={step.question.context}
                                />
                            ) : (
                                <div className="interview-scene">
                                    <Icon name="chat" size={54} />
                                    <p>
                                        Interview question{" "}
                                        {step.question.scenePosition}
                                    </p>
                                </div>
                            )}
                            <p>{step.question.instructions}</p>
                            <div
                                className={`record-status ${mode === "recording" ? "is-recording" : ""}`}
                                role="status"
                            >
                                <Icon name="mic" size={28} />
                                {mode === "permission"
                                    ? "Preparing microphone and question audio…"
                                    : mode === "listening"
                                      ? "Listen to the question. Recording will start when the audio ends."
                                      : mode === "recording"
                                        ? "Speak now — recording"
                                        : mode === "processing"
                                          ? "Preparing your recording…"
                                          : mode === "ready"
                                            ? "Your recording is ready"
                                            : "Start listening when you are ready."}
                            </div>
                            <div
                                className="speaking-timer"
                                aria-label="Recording time remaining"
                            >
                                {mode === "recording"
                                    ? remaining
                                    : step.question.responseSeconds}
                                <span>seconds</span>
                            </div>
                            {recordingUrl && (
                                <div className="recording-preview">
                                    <audio
                                        controls
                                        controlsList="nodownload"
                                        src={recordingUrl}
                                        aria-label="Listen to your response"
                                    />

                                    <p>
                                        Your recording is lost if you leave or
                                        refresh this page before submitting.
                                    </p>
                                </div>
                            )}
                            <div className="speaking-actions">
                                {!active && (
                                    <button
                                        className="primary"
                                        disabled={busy || attempted}
                                        onClick={begin}
                                    >
                                        <Icon name="mic" />
                                        {recording
                                            ? "Record again"
                                            : "Listen and record"}
                                    </button>
                                )}
                                {mode === "recording" && (
                                    <button
                                        className="secondary"
                                        onClick={finishRecording}
                                    >
                                        Finish recording
                                    </button>
                                )}
                                {["permission", "listening"].includes(mode) && (
                                    <button
                                        className="secondary"
                                        onClick={() => {
                                            generation.current++;
                                            stopDevices();
                                            setMode("idle");
                                        }}
                                    >
                                        Cancel
                                    </button>
                                )}
                                {(recording || attempted) && (
                                    <button
                                        className="primary"
                                        disabled={busy || active || !configured}
                                        onClick={() => submit()}
                                    >
                                        {busy
                                            ? "Assessing…"
                                            : attempted
                                              ? "Resubmit the same recording"
                                              : "Assess recording"}
                                    </button>
                                )}
                                {!active && !attempted && (
                                    <button
                                        className="text-button"
                                        disabled={busy}
                                        onClick={() => setConfirm("skip")}
                                    >
                                        Skip without answering
                                    </button>
                                )}
                            </div>
                            {!configured && (
                                <p>
                                    AI assessment is currently unavailable. You
                                    can record and review your response.
                                </p>
                            )}
                            <small>
                                Your recording is transmitted for AI analysis
                                when you submit it. We do not save your audio to
                                server files or databases.
                            </small>
                        </section>
                    )}
                    {complete && (
                        <section className="feedback-card speaking-feedback">
                            <div className="feedback-title">
                                <h2>Your speaking feedback</h2>
                                <div className="score-box">
                                    <strong>{progress?.score}</strong>
                                    <span>/ 5</span>
                                </div>
                            </div>
                            <p className="score-note">
                                Transcript-based content estimate, not a full
                                TOEFL Speaking score. Pronunciation,
                                intelligibility, fluency and intonation are not
                                assessed. Check the transcript for recognition
                                errors.
                            </p>
                            {report ? (
                                <>
                                    <h3>Question / reference sentence</h3>
                                    <p lang="en">{report.prompt}</p>
                                    <h3>Your spoken response</h3>
                                    <p lang="en">
                                        {report.feedback.transcript ||
                                            "No spoken response."}
                                    </p>
                                    <p>{report.feedback.rationale}</p>
                                    {report.feedback.strengths.length > 0 && (
                                        <>
                                            <h3>Strengths</h3>
                                            <ul>
                                                {report.feedback.strengths.map(
                                                    (s, i) => (
                                                        <li key={i}>{s}</li>
                                                    ),
                                                )}
                                            </ul>
                                        </>
                                    )}
                                    {report.feedback.issues.map((issue, i) => (
                                        <div className="speaking-issue" key={i}>
                                            <p>{issue.explanation}</p>
                                            <strong>How to improve</strong>
                                            <p>{issue.suggestion}</p>
                                        </div>
                                    ))}
                                    <h3>For your next attempt</h3>
                                    <ul>
                                        {report.feedback.nextSteps.map(
                                            (s, i) => (
                                                <li key={i}>{s}</li>
                                            ),
                                        )}
                                    </ul>
                                    <p className="speaking-report-meta">
                                        · Detailed feedback is available for up
                                        to 30 minutes.
                                    </p>
                                    <div className="speaking-actions no-print"></div>
                                </>
                            ) : (
                                <p>
                                    Detailed feedback is no longer available.
                                    Your score and progress have been saved.
                                </p>
                            )}
                        </section>
                    )}
                    {complete && (
                        <div className="speaking-actions no-print">
                            {position < run.totalSteps && (
                                <button
                                    className="primary"
                                    onClick={() =>
                                        navigate(
                                            path(module, runId, position + 1),
                                        )
                                    }
                                >
                                    Next activity <Icon name="arrow" />
                                </button>
                            )}
                            {run.isCompleted && (
                                <button
                                    className="secondary"
                                    disabled={busy}
                                    onClick={() => setConfirm("restart")}
                                >
                                    Start a new run
                                </button>
                            )}
                        </div>
                    )}
                    {confirm && (
                        <ConfirmDialog
                            cancelLabel="Cancel"
                            title={
                                confirm === "skip"
                                    ? "Skip this activity?"
                                    : "Start a new run?"
                            }
                            action={
                                confirm === "skip"
                                    ? "Skip without answering"
                                    : "Start a new run"
                            }
                            onCancel={() => setConfirm(null)}
                            onConfirm={() =>
                                confirm === "skip" ? submit(true) : restart()
                            }
                            description={
                                confirm === "skip"
                                    ? "This activity will be completed with a score of 0. No audio will be sent for AI analysis."
                                    : "Your new run starts at 0%. Your previous progress remains saved."
                            }
                        />
                    )}
                </>
            )}
        </>
    );
}
