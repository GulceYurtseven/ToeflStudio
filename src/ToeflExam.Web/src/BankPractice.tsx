import { taskNames, PassageText, vocabularyTargets } from "./TaskPresentation";
import { estimateSection, estimateOverall } from "./examEstimate";
import { MissingLetters } from "./MissingLetters";
import { useCallback, useEffect, useRef, useState } from "react";
import { ApiError, post, request } from "./api";
import {
    ConfirmDialog,
    ErrorBox,
    Spinner,
    navigate,
    Progress as ProgressBar,
    Icon,
} from "./components";
import { preparePcmRecording, waveBase64 } from "./speakingAudio";
import type { PcmRecording } from "./speakingAudio";
import "./bank.css";

type Progress = {
    id: string;
    mode: string;
    form: string;
    completed: boolean;
    completedActivities: number;
    totalActivities: number;
    completionPercent: number;
    nextPosition: number | null;
    deadline: string | null;
    pendingEvaluations: number;
    accessiblePositions: number[];
    canReview?: boolean;
    phase?: string;
    hasSavedReport?: boolean;
};
type Item = {
    id: number;
    prompt: string;
    prefix: string;
    missingLetters: number;
    options: { id: string; text: string }[];
};
type Question = {
    id: string;
    section: string;
    kind: string;
    title: string;
    text: string;
    context: string;
    instructions: string;
    prompt: string;
    prefix: string;
    suffix: string;
    tiles: { id: number; text: string }[];
    questions: Item[];
    requirements: string[];
    posts: { name: string; text: string }[];
    responseSeconds: number;
    scenePosition: number;
};
type Feedback = {
    transcript?: string;
    score: number;
    maxScore: number;
    items: {
        id: number;
        score: number;
        correctAnswer: string;
        explanation: string;
    }[];
};
type Step = {
    position: number;
    question: Question;
    feedback: Feedback | null;
    playAudio?: boolean;
    draft?: Answer | null;
};
type Answer = {
    submissionId: string;
    answers?: Record<number, string>;
    text?: string;
    order?: number[];
    audioBase64?: string;
    skip?: boolean;
};
type Result = { progress: Progress; feedback: Feedback | null };
type Catalog = {
    listening: { kind: string; activities: number; questions: number }[];
    exams: {
        id: string;
        title: string;
        questionsPerPath: number;
        available: boolean;
    }[];
    limitations: string[];
};
const names: Record<string, string> = {
    "choose-response": "Listen and Choose a Response",
    conversation: "Listen to a Conversation",
    announcement: "Listen to an Announcement",
    "academic-talk": "Listen to an Academic Talk",
};
const listeningGuides: Record<string, string> = {
    "choose-response":
        "Listen to a short question or statement and choose the best response. The spoken prompt is not displayed on screen.",
    conversation:
        "Listen to a conversation between two speakers. Answer the questions about their purpose, details, and implied meaning.",
    announcement:
        "Listen to a short announcement and identify its purpose, key details, and any action the listener should take.",
    "academic-talk":
        "Listen to an academic talk. Answer questions about the main idea, supporting details, and organization. No prior subject knowledge is required.",
};
/** Converts an unknown practice failure to a safe user-facing message. */
function errorText(e: unknown) {
    return e instanceof ApiError
        ? ({
              0: "Connection lost. Your response is still in this page. Retry the same submission.",
              410: "The temporary answer workspace has expired. Detailed answers cannot be recovered.",
              429: "Too many requests. Wait a minute, then retry.",
              502: e.message + " Your accepted answers are preserved; retry the evaluation.",
              503: e.message,
          }[e.status] ?? e.message)
        : e instanceof Error
          ? e.message
          : "The operation could not be completed.";
}

/** Displays ordered Listening paths or isolated exam forms with saved progress. */
export function BankDashboard({ mode }: { mode: "listening" | "exam" }) {
    const [catalog, setCatalog] = useState<Catalog | null>(null),
        [history, setHistory] = useState<Record<string, Progress[]>>({}),
        [error, setError] = useState(""),
        [busy, setBusy] = useState(false),
        [ready, setReady] = useState(false);
    const load = useCallback(async () => {
        try {
            const c = await request<Catalog>("/api/sessions/catalog");
            setCatalog(c);
            const forms =
                mode === "exam"
                    ? c.exams.map((x) => x.id)
                    : c.listening.map((x) => x.kind);
            const data = await Promise.all(
                forms.map(
                    async (f) =>
                        [
                            f,
                            await request<Progress[]>(
                                `/api/sessions/${mode}/${f}/history`,
                            ),
                        ] as const,
                ),
            );
            setHistory(Object.fromEntries(data));
        } catch (e) {
            setError(errorText(e));
        }
    }, [mode]);
    useEffect(() => {
        void load();
    }, [load]);
    /** Starts or resumes the selected learning path and navigates to its accessible activity. */
    async function start(form: string, restart?: string) {
        setBusy(true);
        setError("");
        try {
            if (mode === "exam") {
                const stream = await navigator.mediaDevices.getUserMedia({
                    audio: true,
                });
                stream.getTracks().forEach((t) => t.stop());
            }
            const p = await post<Progress>(
                restart
                    ? `/api/sessions/${restart}/restart`
                    : `/api/sessions/${mode}/${form}`,
            );
            navigate(`/session/${p.id}`);
        } catch (e) {
            setError(errorText(e));
        } finally {
            setBusy(false);
        }
    }
    return (
        <section lang="en" className="bank-dashboard">
            {mode === "exam" && (
                <>
                    <div className="page-heading">
                        <div>
                            <span className="eyebrow">
                                READING · LISTENING · WRITING · SPEAKING
                            </span>
                            <h1 id="page-title" tabIndex={-1}>
                                Practice <em>Exams</em>
                            </h1>
                            <p>
                                Three complete practice forms. Each path
                                contains 93 questions, with material kept
                                separate from Studio practice.
                            </p>
                        </div>
                    </div>
                    <div className="bank-notice">
                        <strong>Before you begin</strong>
                        <p>
                            Use headphones and allow microphone access. The
                            timer continues if you leave or reload. Review
                            answers within a Reading module or the
                            sentence-building set. Finishing that group locks
                            it. Listening and Speaking move forward only; time
                            expiry advances automatically. Accepted responses
                            stay in memory during an unfinished exam; server
                            restarts cannot restore that workspace. Your latest
                            completed report is saved to your account. Completing
                            this exam again replaces the previous report.
                            Recordings awaiting analysis are saved with that
                            report and removed after transcription and assessment.
                        </p>
                        <p>
                            These are practice simulations with local timing and
                            routing rules. Speaking feedback assesses transcript
                            content and language; it does not assess
                            pronunciation or fluency.
                        </p>
                        <details>
                            <summary>Format, timing and scoring limits</summary>
                            <ul>
                                {catalog?.limitations.map((x) => (
                                    <li key={x}>{x}</li>
                                ))}
                            </ul>
                        </details>
                        <label>
                            <input
                                type="checkbox"
                                checked={ready}
                                onChange={(e) => setReady(e.target.checked)}
                            />{" "}
                            I have headphones and a microphone, and have read
                            the practice conditions.
                        </label>
                    </div>
                </>
            )}
            {error && (
                <ErrorBox
                    message={error}
                    retryLabel="Reload"
                    retry={() => {
                        setError("");
                        void load();
                    }}
                />
            )}
            {!catalog && !error && <Spinner text="Loading practice…" />}
            {mode === "listening" && catalog && (
                <section
                    className="studio-overview"
                    aria-label="Listening progress"
                >
                    <span className="eyebrow">YOUR PROGRESS</span>
                    <h2>
                        {catalog.listening.reduce(
                            (n, c) =>
                                n +
                                (history[c.kind]?.[0]?.completedActivities ??
                                    0),
                            0,
                        )}{" "}
                        of{" "}
                        {catalog.listening.reduce(
                            (n, c) => n + c.activities,
                            0,
                        )}{" "}
                        activities completed
                    </h2>
                    <p>
                        Finish a path to start a new run. Completion is separate
                        from accuracy.
                    </p>
                    <ProgressBar
                        value={
                            (catalog.listening.reduce(
                                (n, c) =>
                                    n +
                                    (history[c.kind]?.[0]
                                        ?.completedActivities ?? 0),
                                0,
                            ) /
                                Math.max(
                                    1,
                                    catalog.listening.reduce(
                                        (n, c) => n + c.activities,
                                        0,
                                    ),
                                )) *
                            100
                        }
                        label="Listening overall completion"
                    />
                </section>
            )}
            <div
                className={mode === "listening" ? "module-grid" : "bank-cards"}
            >
                {(mode === "exam"
                    ? catalog?.exams.map((x) => ({
                          key: x.id,
                          title: x.title,
                          detail: `${x.questionsPerPath} questions per path`,
                          available: x.available,
                      }))
                    : catalog?.listening.map((x) => ({
                          key: x.kind,
                          title: names[x.kind],
                          detail: `${x.activities} activities · ${x.questions} questions`,
                          available: true,
                      }))
                )?.map((card, cardIndex) => {
                    const latest = history[card.key]?.[0];
                    const savedReport = history[card.key]?.find((run) => run.hasSavedReport);
                    return (
                        <article
                            className={
                                "module-card" +
                                (mode === "listening"
                                    ? ` module-${cardIndex % 3}`
                                    : "")
                            }
                            key={card.key}
                        >
                            {mode === "listening" && (
                                <span className="module-icon">
                                    <Icon name="headphones" size={28} />
                                </span>
                            )}
                            <span className="eyebrow">{card.detail}</span>
                            <h2>{card.title}</h2>
                            {mode === "listening" && (
                                <details className="module-guide">
                                    <summary>Task instructions</summary>
                                    <p>{listeningGuides[card.key]}</p>
                                </details>
                            )}
                            <p>
                                {latest
                                    ? `${latest.completionPercent}% complete`
                                    : "Ready for your first run"}
                            </p>
                            <ProgressBar
                                value={latest?.completionPercent ?? 0}
                                label={`${card.title} completion`}
                            />
                            <div className="bank-actions">
                                <button
                                    className={
                                        mode === "listening"
                                            ? "primary full"
                                            : "primary"
                                    }
                                    disabled={
                                        busy ||
                                        !card.available ||
                                        (mode === "exam" && !latest?.completed && !ready)
                                    }
                                    onClick={() => latest?.completed && mode === "exam"
                                        ? navigate(`/session/${latest.id}`)
                                        : void start(card.key)}
                                >
                                    {latest?.completed
                                        ? latest.hasSavedReport ? "View latest report" : "View completed run"
                                        : latest
                                          ? "Continue"
                                          : "Start"}
                                </button>
                                {latest?.completed && (
                                    <button
                                        className="secondary"
                                        disabled={
                                            busy || (mode === "exam" && !ready)
                                        }
                                        onClick={() =>
                                            void start(card.key, latest.id)
                                        }
                                    >
                                        Start again
                                    </button>
                                )}
                            </div>
                            {mode === "exam" && savedReport && savedReport.id !== latest?.id && (
                                <a className="saved-report-link" href={`#/session/${savedReport.id}`}>
                                    View latest completed report
                                </a>
                            )}
                        </article>
                    );
                })}
            </div>
        </section>
    );
}

/** Runs the current Listening activity or server-timed, section-specific practice exam. */
export function BankPractice({ id }: { id: string }) {
    const [progress, setProgress] = useState<Progress | null>(null),
        [steps, setSteps] = useState<Step[]>([]),
        [index, setIndex] = useState(0),
        [answers, setAnswers] = useState<
            Record<number, Record<number, string>>
        >({}),
        [text, setText] = useState(""),
        [order, setOrder] = useState<number[]>([]),
        [orders, setOrders] = useState<Record<number, number[]>>({}),
        [feedback, setFeedback] = useState<Feedback | null>(null),
        [confirmFinish, setConfirmFinish] = useState(false),
        [error, setError] = useState(""),
        [busy, setBusy] = useState(false),
        [audioUrl, setAudioUrl] = useState(""),
        [played, setPlayed] = useState(false),
        [questionIndex, setQuestionIndex] = useState(0),
        [recording, setRecording] = useState(false),
        [seconds, setSeconds] = useState(0),
        [audioReady, setAudioReady] = useState(false),
        [frozen, setFrozen] = useState(false);
    const audio = useRef<HTMLAudioElement | null>(null),
        capture = useRef<PcmRecording | null>(null),
        recordTimer = useRef<ReturnType<typeof setTimeout> | undefined>(
            undefined,
        ),
        attempt = useRef<{ url: string; body: unknown } | null>(null),
        submitRef = useRef<(skip?: boolean, wave?: string) => Promise<void>>(
            async () => {},
        ),
        generation = useRef(0),
        mounted = useRef(true),
        deadlineSubmitted = useRef<string | null>(null),
        recorded = useRef<string>("");
    const step = steps[index],
        q = step?.question,
        exam = progress?.mode === "exam",
        reviewing = !!progress?.canReview;
    const load = useCallback(async () => {
        setError("");
        try {
            const p = await request<Progress>(`/api/sessions/${id}`);
            if (!mounted.current) return;
            setProgress(p);
            if (p.completed) {
                setSteps([]);
                setFeedback(null);
                return;
            }
            const list = await Promise.all(
                p.accessiblePositions.map((pos) =>
                    request<Step>(`/api/sessions/${id}/steps/${pos}`),
                ),
            );
            if (!mounted.current) return;
            setSteps(list);
            setIndex(0);
            setAnswers(
                Object.fromEntries(
                    list.map((s) => [s.position, s.draft?.answers ?? {}]),
                ),
            );
            setOrders(
                Object.fromEntries(
                    list.map((s) => [s.position, s.draft?.order ?? []]),
                ),
            );
            setText("");
            setOrder(list[0]?.draft?.order ?? []);
            setFeedback(null);
            setPlayed(false);
            setQuestionIndex(0);
            setFrozen(false);
            attempt.current = null;
            recorded.current = "";
        } catch (e) {
            if (mounted.current) setError(errorText(e));
        }
    }, [id]);
    useEffect(() => {
        mounted.current = true;
        void load();
        return () => {
            mounted.current = false;
            generation.current++;
            capture.current?.cancel();
            clearTimeout(recordTimer.current);
            audio.current?.pause();
        };
    }, [load]);
    useEffect(() => {
        if (!progress?.deadline) return;
        /** Refreshes the visible remaining time from the server deadline. */
        const tick = () =>
            setSeconds(
                Math.max(
                    0,
                    Math.ceil(
                        (Date.parse(progress.deadline!) - Date.now()) / 1000,
                    ),
                ),
            );
        tick();
        const t = setInterval(tick, 250);
        return () => clearInterval(t);
    }, [progress?.deadline]);
    useEffect(() => {
        if (
            exam &&
            progress?.deadline &&
            seconds === 0 &&
            Date.now() >= Date.parse(progress.deadline) &&
            !busy &&
            !feedback &&
            step &&
            deadlineSubmitted.current !== progress.deadline
        ) {
            deadlineSubmitted.current = progress.deadline;
            if (recording) capture.current?.stop();
            else void submitRef.current(false, recorded.current);
        }
    }, [seconds, exam, progress?.deadline, busy, feedback, step, recording]);
    useEffect(() => {
        if (!progress || progress.completed) return;
        /** Warns before leaving an unfinished response that cannot be recovered automatically. */
        const warn = (e: BeforeUnloadEvent) => {
            e.preventDefault();
            e.returnValue = "";
        };
        window.addEventListener("beforeunload", warn);
        return () => window.removeEventListener("beforeunload", warn);
    }, [progress]);
    useEffect(() => {
        if (
            !step ||
            step.playAudio === false ||
            !["listening", "speaking"].includes(step.question.section)
        ) {
            setAudioUrl("");
            if (step?.playAudio === false) setPlayed(true);
            return;
        }
        const controller = new AbortController();
        let url = "";
        const current = ++generation.current;
        setAudioReady(false);
        setPlayed(false);
        setRecording(false);
        capture.current?.cancel();
        clearTimeout(recordTimer.current);
        (async () => {
            try {
                const blob = await request<Blob>(
                    `/api/sessions/${id}/steps/${step.position}/audio`,
                    { signal: controller.signal },
                    true,
                    "blob",
                );
                if (current !== generation.current) return;
                url = URL.createObjectURL(blob);
                setAudioUrl(url);
                if (step.question.section === "speaking") {
                    const stream = await navigator.mediaDevices.getUserMedia({
                        audio: {
                            channelCount: 1,
                            echoCancellation: true,
                            noiseSuppression: true,
                        },
                    });
                    if (current !== generation.current) {
                        stream.getTracks().forEach((t) => t.stop());
                        return;
                    }
                    const prepared = await preparePcmRecording(
                        stream,
                        step.question.responseSeconds,
                    );
                    if (current !== generation.current) {
                        prepared.cancel();
                        return;
                    }
                    capture.current = prepared;
                }
                setAudioReady(true);
            } catch (e) {
                if (
                    !controller.signal.aborted &&
                    current === generation.current
                )
                    setError(errorText(e));
            }
        })();
        return () => {
            controller.abort();
            generation.current++;
            capture.current?.cancel();
            clearTimeout(recordTimer.current);
            if (url) URL.revokeObjectURL(url);
        };
    }, [id, step?.position]);
    useEffect(() => {
        if (audioReady && exam && audio.current) {
            void audio.current.play().catch(() => {});
        }
    }, [audioReady, audioUrl, exam]);
    /** Saves the current objective draft without scoring it or resetting the shared phase deadline. */
    const saveDraft = useCallback(async () => {
        if (!reviewing || !step || frozen) return;
        await post(`/api/sessions/${id}/steps/${step.position}/draft`, {
            submissionId: crypto.randomUUID(),
            answers: answers[step.position] ?? {},
            order: q?.kind === "build-sentence" ? order : undefined,
        });
    }, [reviewing, step?.position, frozen, id, answers, order, q?.kind]);
    useEffect(() => {
        if (!reviewing || busy || frozen) return;
        const timer = setTimeout(() => {
            void saveDraft().catch((e) => setError(errorText(e)));
        }, 700);
        return () => clearTimeout(timer);
    }, [saveDraft, reviewing, busy, frozen]);
    /** Moves within the open phase only, retaining both the previous and next response. */
    async function moveTo(next: number) {
        if (busy || frozen || next < 0 || next >= steps.length) return;
        setBusy(true);
        try {
            await saveDraft();
            setOrders((old) => ({ ...old, [step.position]: order }));
            setOrder(orders[steps[next].position] ?? []);
            setIndex(next);
            setError("");
        } catch (e) {
            setError(errorText(e));
        } finally {
            setBusy(false);
        }
    }
    /** Submits only the current exam question and preserves the frozen payload for retries. */
    async function send(skip = false, wave = "") {
        if (!progress || !step || busy) return;
        setConfirmFinish(false);
        setBusy(true);
        setFrozen(true);
        setError("");
        if (!attempt.current) {
            const key = crypto.randomUUID();
            attempt.current = reviewing
                ? {
                      url: `/api/sessions/${id}/finish-phase`,
                      body: {
                          anchorPosition: progress.nextPosition,
                          responses: Object.fromEntries(
                              steps.map((s) => [
                                  s.position,
                                  {
                                      submissionId: crypto.randomUUID(),
                                      answers: answers[s.position] ?? {},
                                      order:
                                          s.question.kind === "build-sentence"
                                              ? s.position === step.position
                                                  ? order
                                                  : (orders[s.position] ?? [])
                                              : undefined,
                                  },
                              ]),
                          ),
                      },
                  }
                : {
                      url: `/api/sessions/${id}/steps/${step.position}`,
                      body: {
                          submissionId: key,
                          answers: answers[step.position] ?? {},
                          text,
                          order,
                          audioBase64: wave || recorded.current,
                          skip,
                      } satisfies Answer,
                  };
        }
        try {
            const result = await post<Result>(
                attempt.current.url,
                attempt.current.body,
            );
            if (!mounted.current) return;
            setProgress(result.progress);
            if (result.feedback) {
                setFeedback(result.feedback);
                setFrozen(true);
            } else await load();
        } catch (e) {
            if (mounted.current) {
                if (e instanceof ApiError && e.status === 409) await load();
                else setError(errorText(e));
            }
        } finally {
            if (mounted.current) setBusy(false);
        }
    }
    submitRef.current = send;
    /** Finishes audio playback and starts the appropriate response or microphone phase. */
    async function ended() {
        setPlayed(true);
        if (q?.section !== "speaking" || !capture.current) return;
        const current = generation.current;
        setRecording(true);
        try {
            const pending = capture.current.start();
            recordTimer.current = setTimeout(
                () => capture.current?.stop(),
                q.responseSeconds * 1000,
            );
            const wave = await pending;
            clearTimeout(recordTimer.current);
            if (current !== generation.current) return;
            setRecording(false);
            recorded.current = await waveBase64(wave);
            await submitRef.current(false, recorded.current);
        } catch (e) {
            if (current === generation.current) {
                setRecording(false);
                setError(errorText(e));
            }
        }
    }
    /** Updates the current question response without changing frozen submissions. */
    const changeAnswer = (item: number, value: string) => {
        if (!step || frozen) return;
        setAnswers((old) => ({
            ...old,
            [step.position]: { ...old[step.position], [item]: value },
        }));
    };
    if (progress?.completed && !feedback)
        return exam ? (
            <ExamReportView id={id} />
        ) : (
            <section lang="en">
                <h1 id="page-title" tabIndex={-1}>
                    Path complete
                </h1>
                <p>You have completed 100% of this path.</p>
                <div className="bank-actions">
                    <button
                        className="primary"
                        onClick={async () => {
                            try {
                                const p = await post<Progress>(
                                    `/api/sessions/${id}/restart`,
                                );
                                navigate(`/session/${p.id}`);
                            } catch (e) {
                                setError(errorText(e));
                            }
                        }}
                    >
                        Start a new run
                    </button>
                    <a className="secondary" href="#/listening">
                        Listening Studio
                    </a>
                </div>
                {error && <ErrorBox message={error} />}
            </section>
        );
    if (!q || !step)
        return (
            <>
                {error ? (
                    <ErrorBox
                        message={error}
                        retryLabel="Reload"
                        retry={() => void load()}
                    />
                ) : (
                    <Spinner text="Loading your session…" />
                )}
            </>
        );
    const fields =
        q.section === "listening"
            ? q.questions.slice(questionIndex, questionIndex + 1)
            : q.questions;
    const canAnswer = q.section !== "listening" || played || !!feedback;
    const hasResponse =
        q.questions.length > 0
            ? q.questions.every(
                  (item) => !!answers[step.position]?.[item.id]?.trim(),
              )
            : q.kind === "build-sentence"
              ? order.length > 0
              : !!text.trim();
    return (
        <section lang="en" className="bank-practice">
            {confirmFinish && <ConfirmDialog title="Finish this module?"
                description="Your answers will be submitted and the next module will begin. You cannot return to this module. The exam timer continues while this dialog is open."
                cancelLabel="Keep reviewing" action="Submit module"
                onCancel={() => setConfirmFinish(false)}
                onConfirm={() => { setConfirmFinish(false); void send(); }} />}

            <div className="bank-session-heading">
                <div>
                    <span className="eyebrow">
                        {exam ? "PRACTICE EXAM" : "LISTENING STUDIO"} ·{" "}
                        {progress?.completionPercent}%
                    </span>
                    <h1 id="page-title" tabIndex={-1}>
                        {taskNames[q.kind] ?? q.title}
                    </h1>
                    {!/^Exam sentence/i.test(q.title) && q.title !== taskNames[q.kind] && <p>{q.title}</p>}
                </div>
                {progress?.deadline && (
                    <div className="bank-timer" aria-label="Time remaining">
                        {Math.floor(seconds / 60)}:
                        {String(seconds % 60).padStart(2, "0")}
                    </div>
                )}
            </div>
            <progress
                max={100}
                value={progress?.completionPercent ?? 0}
                aria-label="Session completion"
            />
            {error && (
                <ErrorBox
                    message={error}
                    retryLabel={
                        attempt.current ? "Retry submission" : "Reload session"
                    }
                    retry={() => (attempt.current ? void send() : void load())}
                />
            )}
            <p>
                {q.kind === "build-sentence" ? "Arrange the tiles to build a grammatically correct sentence. Some tiles may not be needed." : q.instructions ||
                    "Read the material and answer the questions."}
            </p>
            {q.context && <div className="bank-notice">{q.context}</div>}
            {q.kind === "listen-repeat" && (
                <div
                    className="bank-scene"
                    aria-label={`Guided visit, stop ${q.scenePosition} of seven`}
                >
                    {Array.from({ length: 7 }, (_, i) => (
                        <span
                            className={
                                i + 1 === q.scenePosition ? "current" : ""
                            }
                            key={i}
                        >
                            {i + 1}
                        </span>
                    ))}
                </div>
            )}
            {audioUrl && (
                <div className="bank-audio">
                    <audio
                        ref={audio}
                        src={audioUrl}
                        onEnded={() => void ended()}
                        onError={() =>
                            setError(
                                "The question audio could not be played. Reload this session.",
                            )
                        }
                        controls={!exam}
                        controlsList="nodownload"
                    />
                    {exam && !played && !recording && (
                        <button
                            className="secondary"
                            disabled={!audioReady || busy}
                            onClick={() =>
                                void audio.current
                                    ?.play()
                                    .catch((e) => setError(errorText(e)))
                            }
                        >
                            Play question audio
                        </button>
                    )}
                    {q.section === "speaking" && (
                        <p role="status">
                            {recording
                                ? "Recording… Your response will be submitted automatically."
                                : played
                                  ? "Submitting recording…"
                                  : "Listen first. Recording begins immediately after the prompt."}
                        </p>
                    )}
                </div>
            )}
            {q.section === "speaking" && recording && (
                <button
                    className="secondary"
                    onClick={() => capture.current?.stop()}
                >
                    Finish response
                </button>
            )}
            {q.text && (
                <div className="bank-passage">
                    {q.text.split(/(\{\{\d+\}\})/).map((part, i) => {
                        const match = part.match(/^\{\{(\d+)\}\}$/);
                        if (!match) return <PassageText key={i} text={part} targets={vocabularyTargets(fields)} />;
                        const item = q.questions.find(
                            (x) => x.id === Number(match[1]),
                        );
                        return item ? (
                            <label className="bank-gap" key={i}>
                                <span>{item.prefix}</span>
                                <MissingLetters
                                    count={item.missingLetters}
                                    label={`Missing letters ${item.id}`}
                                    value={answers[step.position]?.[item.id] ?? ""}
                                    disabled={busy || frozen}
                                    onChange={value => changeAnswer(item.id, value)}
                                />
                            </label>
                        ) : null;
                    })}
                </div>
            )}
            {q.prompt && (
                <p>
                    <strong>{q.prompt}</strong>
                </p>
            )}
            {canAnswer &&
                fields
                    .filter((x) => x.options.length > 0)
                    .map((item) => (
                        <fieldset
                            className="bank-question"
                            disabled={busy || frozen}
                            key={item.id}
                        >
                            <legend>
                                {item.id}. {item.prompt}
                            </legend>
                            {item.options.map((option) => (
                                <label
                                    key={option.id}
                                    className={
                                        answers[step.position]?.[item.id] ===
                                        option.id
                                            ? "selected"
                                            : ""
                                    }
                                >
                                    <input
                                        type="radio"
                                        name={`q-${step.position}-${item.id}`}
                                        checked={
                                            answers[step.position]?.[
                                                item.id
                                            ] === option.id
                                        }
                                        onChange={() =>
                                            changeAnswer(item.id, option.id)
                                        }
                                    />
                                    <span>{option.text}</span>
                                </label>
                            ))}
                        </fieldset>
                    ))}
            {q.kind === "build-sentence" && (
                <div className="bank-sentence">
                    <p>
                        {q.prefix}
                        {order.map((n, i) => (
                            <button
                                className="secondary"
                                key={i}
                                disabled={busy || frozen}
                                onClick={() =>
                                    setOrder(order.filter((_, j) => j !== i))
                                }
                            >
                                {q.tiles.find((t) => t.id === n)?.text} ×
                            </button>
                        ))}
                        {q.suffix}
                    </p>
                    <div className="bank-actions">
                        {q.tiles.map((t) => (
                            <button
                                className="secondary"
                                key={t.id}
                                disabled={
                                    busy || frozen || order.includes(t.id)
                                }
                                onClick={() => setOrder([...order, t.id])}
                            >
                                {t.text}
                            </button>
                        ))}
                    </div>
                    <p className="muted">
                        Use the needed tiles in order. One tile is not needed.
                    </p>
                </div>
            )}
            {!!q.requirements.length && (
                <ul>
                    {q.requirements.map((r) => (
                        <li key={r}>{r}</li>
                    ))}
                </ul>
            )}
            {q.posts.map((p) => (
                <blockquote key={p.name}>
                    <strong>{p.name}</strong>
                    <p>{p.text}</p>
                </blockquote>
            ))}
            {["email", "academic-discussion"].includes(q.kind) && (
                <label className="bank-writing">
                    Your response
                    <textarea
                        rows={14}
                        maxLength={20000}
                        value={text}
                        onChange={(e) => setText(e.target.value)}
                        disabled={busy || frozen}
                        spellCheck={false}
                    />
                    <span>
                        {text.trim() ? text.trim().split(/\s+/).length : 0}{" "}
                        words
                    </span>
                </label>
            )}
            {feedback ? (
                <div className="bank-feedback" role="status">
                    <h2>
                        {feedback.score} / {feedback.maxScore}
                    </h2>
                    {feedback.items.map((i) => (
                        <p key={i.id}>
                            <strong>
                                {i.score ? "Correct" : "Review"} ·{" "}
                                {i.correctAnswer}
                            </strong>
                            <br />
                            {i.explanation}
                        </p>
                    ))}
                    <details>
                        <summary>Transcript</summary>
                        <p className="bank-passage">{feedback.transcript}</p>
                    </details>
                    <button className="primary" onClick={() => void load()}>
                        Next activity
                    </button>
                </div>
            ) : (
                <div className="bank-actions no-print">
                    {reviewing && (
                        <>
                            <button
                                className="secondary"
                                disabled={busy || frozen || index === 0}
                                onClick={() => void moveTo(index - 1)}
                            >
                                Back
                            </button>
                            <span role="status">
                                Item {index + 1} of {steps.length} in this{" "}
                                {q.section === "reading" ? "module" : "set"}
                            </span>
                            <button
                                className="secondary"
                                disabled={
                                    busy || frozen || index === steps.length - 1
                                }
                                onClick={() => void moveTo(index + 1)}
                            >
                                Next
                            </button>
                            <button
                                className="primary"
                                disabled={busy}
                                onClick={() => {
                                    if (frozen) void send();
                                    else setConfirmFinish(true);
                                }}
                            >
                                {frozen ? "Retry submission" : "Finish module"}
                            </button>
                        </>
                    )}
                    {!reviewing && q.section !== "speaking" && (
                        <button
                            className="primary"
                            disabled={
                                busy ||
                                (!canAnswer && !frozen) ||
                                (exam && !frozen && !hasResponse)
                            }
                            onClick={() => {
                                if (
                                    q.section === "listening" &&
                                    questionIndex < q.questions.length - 1 &&
                                    !frozen
                                )
                                    setQuestionIndex(questionIndex + 1);
                                else void send();
                            }}
                        >
                            {busy
                                ? "Submitting…"
                                : frozen
                                  ? "Retry submission"
                                  : exam && q.section === "reading"
                                    ? "Submit and continue"
                                    : q.section === "listening" &&
                                        questionIndex < q.questions.length - 1
                                      ? "Next question"
                                      : "Submit and continue"}
                        </button>
                    )}
                    {!exam && !frozen && (
                        <button
                            className="text-button"
                            disabled={busy || recording}
                            onClick={() => {
                                capture.current?.cancel();
                                audio.current?.pause();
                                void send(true);
                            }}
                        >
                            Leave unanswered
                        </button>
                    )}
                </div>
            )}
        </section>
    );
}

type ReportDetail = {
    instructions?: string;
    questions?: Item[];
    requirements?: string[];
    posts?: { name: string; text: string }[];
    position: number;
    section: string;
    kind: string;
    title: string;
    prompt: string;
    passage: string;
    timedOut: boolean;
    answer: {
        answers?: Record<string, string>;
        text?: string;
        order?: number[];
    } | null;
    score: number | null;
    maximum: number;
    pending: boolean;
    feedback: Record<string, unknown> | null;
};
type StudyPlan = { sections: { section: string; summary: string; priorities: { skill: string; evidence: string; positions: number[]; exercise: string; successCheck: string }[] }[] };
type Report = {
    id: string;
    form: string;
    expiresAt: string | null;
    sections: {
        section: string;
        score: number;
        maximum: number;
        pending: number;
    }[];
    details: ReportDetail[];
    limitations: string[];
    detailsUnavailable: boolean;
};
/** Renders assessment data as escaped React text rather than executable HTML. */
function FeedbackText({ value }: { value: unknown }) {
    if (value == null) return null;
    if (typeof value === "string" || typeof value === "number")
        return <span>{String(value)}</span>;
    if (Array.isArray(value))
        return (
            <ul>
                {value.map((v, i) => (
                    <li key={i}>
                        <FeedbackText value={v} />
                    </li>
                ))}
            </ul>
        );
    if (typeof value === "object")
        return (
            <dl>
                {Object.entries(value)
                    .filter(
                        ([k]) =>
                            !["id", "score", "maxScore", "category"].includes(
                                k,
                            ),
                    )
                    .map(([key, v]) => (
                        <div key={key}>
                            <dt>{key.replace(/([A-Z])/g, " $1")}</dt>
                            <dd>
                                <FeedbackText value={v} />
                            </dd>
                        </div>
                    ))}
            </dl>
        );
    return null;
}
/** Loads final exam feedback and exposes the exam-only PDF print report. */
function ExamReportView({ id }: { id: string }) {
    const [studyPlan, setStudyPlan] = useState<StudyPlan | null>(null);
    const [studyError, setStudyError] = useState("");
    const [studyBusy, setStudyBusy] = useState(false);
    const [studyRetry, setStudyRetry] = useState(0);
    const [report, setReport] = useState<Report | null>(null),
        [error, setError] = useState(""),
        [busy, setBusy] = useState(false),
        [refresh, setRefresh] = useState(0);
    useEffect(() => {
        let active = true;
        const controller = new AbortController();
        setError("");
        setBusy(true);
        (async () => {
            try {
                let result = await request<Report>(
                    `/api/sessions/${id}/report`,
                    { signal: controller.signal },
                );
                if (active) setReport(result);
                while (
                    active &&
                    !result.detailsUnavailable &&
                    result.sections.some((s) => s.pending > 0)
                ) {
                    result = await request<Report>(
                        `/api/sessions/${id}/evaluate`,
                        { method: "POST", signal: controller.signal },
                    );
                    if (active) setReport(result);
                    if (result.sections.some((s) => s.pending > 0))
                        await new Promise((resolve) =>
                            setTimeout(resolve, 5100),
                        );
                }
            } catch (e) {
                if (active) setError(errorText(e));
            } finally {
                if (active) setBusy(false);
            }
        })();
        return () => {
            active = false;
            controller.abort();
        };
    }, [id, refresh]);

    const readyForGuidance = !!report && !report.detailsUnavailable && report.sections.length === 4 && report.sections.every(s => s.pending === 0);
    // Advice is independent of grading: failed generation never hides answers or changes scores.
    useEffect(() => {
        if (!readyForGuidance) return;
        const controller = new AbortController();
        setStudyBusy(true); setStudyError("");
        request<StudyPlan>(`/api/sessions/${id}/study-plan`, {method: "POST", signal: controller.signal})
            .then(plan => { if (!controller.signal.aborted) setStudyPlan(plan); })
            .catch(e => { if (!controller.signal.aborted) setStudyError(errorText(e)); })
            .finally(() => { if (!controller.signal.aborted) setStudyBusy(false); });
        return () => controller.abort();
    }, [id, readyForGuidance, studyRetry]);

    return (
        <section className="bank-report" lang="en">
            <span className="eyebrow">TOEFL PRACTICE EXAM</span>
            <h1 id="page-title" tabIndex={-1}>
                Your exam report
            </h1>
            <p>
                Your answers are scored below. Writing task estimates use the
                TOEFL task rubrics; Speaking estimates cover transcript content
                only. All band estimates below are unofficial.
            </p>
            {error && (
                <ErrorBox
                    message={error}
                    retryLabel="Retry evaluation"
                    retry={() => setRefresh((n) => n + 1)}
                />
            )}
            {busy && (
                <p role="status">
                    Evaluating responses…{" "}
                    {report?.sections.reduce((n, s) => n + s.pending, 0) ?? "—"}{" "}
                    remaining. You can keep this page open.
                </p>
            )}
            {report && (
                <>
                    <section className="bank-notice" aria-label="Estimated TOEFL score">
                        <h2>AI-assisted TOEFL estimate</h2>
                        <strong className="exam-estimate-value">
                            {estimateOverall(report.sections) === null
                                ? "Overall estimate unavailable until all four sections are evaluated"
                                : `${estimateOverall(report.sections)!.toFixed(1)} / 6`}
                        </strong>
                        <p>
                            This estimate uses AI-generated Writing and Speaking assessments
                            together with automatically marked Reading and Listening answers.
                            AI can make mistakes. This is not an official ETS score and may
                            differ substantially from your actual TOEFL result.
                        </p>
                        <p>
                            Speaking covers transcript content only; pronunciation and spoken
                            fluency are not assessed. Do not use this estimate as proof of
                            English proficiency or a guarantee of test performance.
                        </p>
                        <details>
                            <summary>How this estimate is calculated</summary>
                            <p>
                                Each section uses 1 + 5 × (raw points ÷ maximum points),
                                rounded to the nearest 0.5. The overall estimate is the equally
                                weighted average of all four section estimates, rounded to
                                the nearest 0.5; ties round up. A zero raw score maps to 1.0.
                                This is our uncalibrated practice formula, not an ETS conversion.
                                It does not account for adaptive question difficulty or official
                                score equating. Pending or failed evaluations never count as zero.
                            </p>
                        </details>
                    </section>
                    <div className="bank-cards">
                        {report.sections.map((s) => (
                            <article className="module-card" key={s.section}>
                                <h2>{s.section}</h2>
                                <p>
                                    Estimated band: {estimateSection(s) === null
                                        ? "Pending / unavailable"
                                        : `${estimateSection(s)!.toFixed(1)} / 6`}
                                </p>
                                <strong>
                                    {s.pending
                                        ? `${s.pending} awaiting analysis`
                                        : `${s.score} / ${s.maximum}`}
                                </strong>
                                <p>
                                    {s.section === "speaking"
                                        ? "Transcript content estimate; delivery unassessed"
                                        : s.section === "writing"
                                          ? "Raw points: Build a Sentence 0–1 per item; Email and Academic Discussion 0–5 each."
                                          : "Raw points: 1 per correct answer; 0 per incorrect or unanswered item."}
                                </p>
                            </article>
                        ))}
                    </div>
                    {report.detailsUnavailable && (
                        <p className="bank-notice">
                            Detailed answers are unavailable for this older run.
                            Its saved scores are shown below. New completed exams
                            keep their latest report in your account.
                        </p>
                    )}
                    <div className="bank-actions no-print">
                        <button
                            className="primary"
                            disabled={busy}
                            onClick={() => window.print()}
                        >
                            Save as PDF
                        </button>

                        <a className="secondary" href="#/exams">
                            All practice exams
                        </a>
                    </div>
                    {studyBusy && <p role="status">Creating your personalized study plan…</p>}
                    {studyError && <ErrorBox message={studyError} retryLabel="Retry study plan" retry={() => setStudyRetry(n => n + 1)} />}
                    {report.sections.map(section => <section className="exam-section-review" key={section.section}>
                        <h2 className="section-review-title">{section.section} review</h2>
                        {report.details.filter(d => d.section === section.section).map((d) => (
                        <article className="bank-report-item" key={d.position}>
                            <span className="eyebrow">
                                {d.section} · {taskNames[d.kind] ?? d.kind}
                            </span>
                            <h2>{/^Exam sentence/i.test(d.title) ? taskNames[d.kind] : d.title}</h2>
                            <p>
                                {d.pending
                                    ? "Awaiting analysis"
                                    : `${d.score} / ${d.maximum}`}
                                {d.timedOut ? " · Time expired" : ""}
                            </p>
                            {d.prompt && <p>{d.prompt}</p>}
                            {d.instructions && <p>{d.instructions}</p>}
                            {!!d.requirements?.length && (
                                <ul>
                                    {d.requirements.map((r) => (
                                        <li key={r}>{r}</li>
                                    ))}
                                </ul>
                            )}
                            {d.posts?.map((p) => (
                                <blockquote key={p.name}>
                                    <strong>{p.name}</strong>
                                    <p>{p.text}</p>
                                </blockquote>
                            ))}
                            {d.passage && (
                                <p className="bank-passage"><PassageText text={d.passage} targets={vocabularyTargets(d.questions ?? [])} /></p>
                            )}
                            {d.questions?.length ? (
                                <div className="response-review">
                                    <h3>Your responses</h3>
                                    {d.questions.map(q => {
                                        const result = (d.feedback?.items as {id:number; correctAnswer:string; explanation:string; score:number}[] | undefined)?.find(i => i.id === q.id);
                                        const response = d.answer?.answers?.[q.id];
                                        return <div className="response-review-row" key={q.id}>
                                            <h3>{q.id}. {d.kind === "complete-words" ? `${q.prefix}${"_".repeat(q.missingLetters)}` : q.prompt}</h3>
                                            {q.options.map(o => <p key={o.id}>{o.id}. {o.text}</p>)}
                                            <p><strong>Your response: </strong>{response?.trim() ? (d.kind === "complete-words" ? q.prefix + response : response) : "No response"}</p>
                                            {result && <>
                                                <p><strong>{result.score ? "Correct" : "Correct answer"}: </strong>{result.correctAnswer}</p>
                                                <p>{result.explanation}</p>
                                            </>}
                                        </div>;
                                    })}
                                </div>
                            ) : <>
                                <h3>Your response</h3>
                                <p className="bank-passage">{d.answer?.text?.trim() || (d.pending && d.section === "speaking" ? "Recording received; transcription is pending." : "No response")}</p>
                            </>}
                            {!d.questions?.length && <FeedbackText value={d.feedback} />}

                        </article>
                        ))}
                        {studyPlan?.sections?.filter(s => s.section === section.section).map(s => <aside className="study-guidance" key={s.section}>
                            <h3>What to study next · {s.section}</h3>
                            <p className="eyebrow">AI-generated guidance · may contain mistakes</p>
                            <p>{s.summary}</p>
                            {s.priorities.map((p, i) => <div key={i}>
                                <h4>{i + 1}. {p.skill}</h4>
                                <p><strong>Based on your answers: </strong>{p.evidence} (Items {p.positions.join(", ")})</p>
                                <p><strong>Practice: </strong>{p.exercise}</p>
                                <p><strong>Check your progress: </strong>{p.successCheck}</p>
                            </div>)}
                        </aside>)}
                    </section>)}
                    <div className="bank-report-limits">
                        <h2>Interpretation and limitations</h2>
                        <ul>
                            {report.limitations.map((l) => (
                                <li key={l}>{l}</li>
                            ))}
                        </ul>
                    </div>
                </>
            )}
        </section>
    );
}
