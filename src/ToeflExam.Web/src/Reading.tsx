import { PassageText, vocabularyTargets } from "./TaskPresentation";
import { MissingLetters } from "./MissingLetters";
import { useEffect, useState } from "react";
import { TestFormat } from "./TestFormat";
import { ApiError, post, request } from "./api";
import {
    ConfirmDialog,
    ErrorBox,
    Icon,
    navigate,
    Progress,
    Spinner,
} from "./components";

export const readingModules = {
    "complete-words": {
        title: "Complete the Words",
        icon: "tiles",
        description: "Use context to complete the missing letters.",
        guide: "The first sentence is complete. After that, the second half of every second word is missing. Fill 10 gaps with only the missing letters. Each correct word earns one raw point.",
    },
    "daily-life": {
        title: "Read in Daily Life",
        icon: "mail",
        description: "Understand emails, notices, and everyday messages.",
        guide: "Read a 15–150-word everyday text and answer two or three multiple-choice questions. Use the text to identify purpose, details, implications, and expressions. Each correct answer earns one raw point.",
    },
    "academic-passage": {
        title: "Read an Academic Passage",
        icon: "book",
        description:
            "Explore main ideas, details, inferences, and vocabulary in context.",
        guide: "Read an approximately 200-word passage and answer five questions. No prior knowledge is required. For sentence insertion, select the best position from [A]–[D]. Each correct answer earns one raw point.",
    },
};
type ReadingModule = keyof typeof readingModules;
/** Narrows a route value to one of the supported Reading module names. */
export const isReadingModule = (value: string): value is ReadingModule =>
    Object.hasOwn(readingModules, value);
type ModuleProgress = {
    module: ReadingModule;
    totalSteps: number;
    completedSteps: number;
    completionPercent: number;
    runId: string | null;
};
type Run = {
    id: string;
    module: ReadingModule;
    isCompleted: boolean;
    completedSteps: number;
    totalSteps: number;
    completionPercent: number;
    score: number;
    maxScore: number;
    nextPosition: number | null;
    steps: {
        position: number;
        status: string;
        score: number | null;
        maxScore: number | null;
    }[];
};
type Item = {
    id: number;
    prompt: string;
    prefix: string;
    missingLetters: number;
    options: { id: string; text: string }[];
};
type Feedback = {
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
    question: {
        id: string;
        title: string;
        format: string;
        text: string;
        items: Item[];
    };
    feedback: Feedback | null;
};
type Draft = {
    answers: Record<number, string>;
    submissionId: string;
    attempted: boolean;
};
/** Builds the Reading runs API route for a validated module. */
const root = (module: ReadingModule) => "/api/reading/" + module + "/runs";
/** Builds the Reading practice hash route for a run position. */
const route = (module: ReadingModule, id: string, position: number) =>
    "/reading/" + module + "/" + id + "/" + position;

/** Restores the current Reading answers and their submission identifier from this tab. */
function readDraft(key: string): Draft {
    try {
        const value = JSON.parse(sessionStorage.getItem(key) || "null");
        if (
            value &&
            typeof value.submissionId === "string" &&
            value.answers &&
            typeof value.answers === "object" &&
            !Array.isArray(value.answers) &&
            Object.values(value.answers).every((v) => typeof v === "string")
        )
            return { ...value, attempted: value.attempted === true };
    } catch {
        /* A blocked store does not prevent practice. */
    }
    return { answers: {}, submissionId: crypto.randomUUID(), attempted: false };
}
/** Saves Reading answers in tab storage while tolerating unavailable browser storage. */
function persist(key: string, draft: Draft) {
    try {
        sessionStorage.setItem(key, JSON.stringify(draft));
        return true;
    } catch {
        return false;
    }
}
/** Removes a completed Reading draft from tab storage. */
function clear(key: string) {
    try {
        sessionStorage.removeItem(key);
    } catch {
        /* No stored draft to remove. */
    }
}

/** Displays ordered Reading material and records objective feedback and progress. */
export function ReadingPractice({
    module,
    runId,
    position,
    email,
}: {
    module: ReadingModule;
    runId: string;
    position: number;
    email: string;
}) {
    const url = root(module) + "/" + runId;
    const key = "toefl.draft.reading." + email + "." + runId + "." + position;
    const [draft, setDraft] = useState(() => readDraft(key));
    const [run, setRun] = useState<Run | null>(null);
    const [step, setStep] = useState<Step | null>(null);
    const [error, setError] = useState("");
    const [busy, setBusy] = useState(false);
    const [loading, setLoading] = useState(true);
    const [stored, setStored] = useState(true);
    const [revision, reload] = useState(0);
    const [confirm, setConfirm] = useState<"submit" | "restart" | null>(null);
    const complete =
        run?.steps.find((s) => s.position === position)?.status === "completed";
    useEffect(() => {
        const controller = new AbortController();
        setLoading(true);
        setError("");
        Promise.all([
            request<Run>(url, { signal: controller.signal }),
            request<Step>(url + "/steps/" + position, {
                signal: controller.signal,
            }),
        ])
            .then(([r, s]) => {
                setRun(r);
                setStep(s);
            })
            .catch((e) => {
                if (!controller.signal.aborted) setError(e.message);
            })
            .finally(() => {
                if (!controller.signal.aborted) setLoading(false);
            });
        return () => controller.abort();
    }, [url, position, revision]);
    useEffect(() => {
        if (complete) clear(key);
        else setStored(persist(key, draft));
    }, [draft, complete, key]);
    const locked = busy || !!complete || draft.attempted;
    /** Updates the editable response for a single Reading item. */
    function answer(id: number, value: string) {
        if (!locked)
            setDraft((d) => ({ ...d, answers: { ...d.answers, [id]: value } }));
    }
    /** Freezes the current response and submits it with the same identifier on retries. */
    async function submit() {
        setConfirm(null);
        setBusy(true);
        setError("");
        const frozen = { ...draft, attempted: true };
        setDraft(frozen);
        setStored(persist(key, frozen));
        try {
            const result = await post<{ progress: Run; feedback: Feedback }>(
                url + "/steps/" + position + "/submissions",
                { submissionId: frozen.submissionId, answers: frozen.answers },
            );
            setRun(result.progress);
            setStep((s) => s && { ...s, feedback: result.feedback });
            clear(key);
            setTimeout(
                () => document.getElementById("reading-feedback")?.focus(),
                0,
            );
        } catch (e) {
            setError((e as Error).message);
            if (e instanceof ApiError && e.status === 400)
                setDraft((d) => ({ ...d, attempted: false }));
        } finally {
            setBusy(false);
        }
    }
    /** Starts a fresh run after completion and navigates to its first activity. */
    async function restart() {
        setConfirm(null);
        setBusy(true);
        setError("");
        try {
            const next = await post<Run>(url + "/restart");
            navigate(route(module, next.id, 1));
        } catch (e) {
            setError((e as Error).message);
        } finally {
            setBusy(false);
        }
    }
    /** Splits authored cloze text into escaped text and missing-word input controls. */
    function cloze(text: string) {
        return text.split(/(\{\{\d+\}\})/).map((part, index) => {
            const match = part.match(/^\{\{(\d+)\}\}$/);
            if (!match) return <span key={index}>{part}</span>;
            const item = step!.question.items.find(
                (i) => i.id === Number(match[1]),
            )!;
            return (
                <span className="cloze-word" key={index}>
                    <span>{item.prefix}</span>
                    <MissingLetters
                        count={item.missingLetters}
                        value={draft.answers[item.id] || ""}
                        label={`Gap ${item.id}, ${item.missingLetters} missing letters`}
                        disabled={locked}
                        onChange={value => answer(item.id, value)}
                    />
                    <sup>{item.id}</sup>
                </span>
            );
        });
    }
    return (
        <>
            <div className="practice-top no-print">
                <a className="text-button" href="#/reading">
                    <Icon name="back" />
                    Back to Reading Studio{" "}
                </a>
                <span className="mode-badge">Untimed practice</span>
            </div>
            <div className="practice-heading">
                <h1 id="page-title" tabIndex={-1}>
                    {readingModules[module].title}
                </h1>
                {run && (
                    <span className="step-caption">
                        ACTIVITY <strong>{position}</strong> / {run.totalSteps}
                    </span>
                )}
            </div>
            {error && (
                <ErrorBox message={error} retry={() => reload(revision + 1)} />
            )}
            {loading ? (
                <Spinner />
            ) : (
                step &&
                run && (
                    <>
                        <div
                            className="reading-path no-print"
                            aria-label="Reading activities"
                        >
                            {run.steps.map((s) => (
                                <button
                                    className={
                                        "reading-step " +
                                        (s.position === position
                                            ? "current"
                                            : "") +
                                        (s.status === "completed"
                                            ? " done"
                                            : "")
                                    }
                                    key={s.position}
                                    disabled={s.status === "locked" || busy}
                                    aria-current={
                                        s.position === position
                                            ? "step"
                                            : undefined
                                    }
                                    aria-label={
                                        "Activity " +
                                        s.position +
                                        (s.status === "completed"
                                            ? ", completed"
                                            : s.status === "locked"
                                              ? ", locked"
                                              : "")
                                    }
                                    onClick={() =>
                                        navigate(
                                            route(module, run.id, s.position),
                                        )
                                    }
                                >
                                    {s.status === "completed" ? (
                                        <Icon name="check" size={16} />
                                    ) : (
                                        s.position
                                    )}
                                </button>
                            ))}
                            <div className="reading-path-progress">
                                <Progress
                                    value={run.completionPercent}
                                    label="Reading run completion"
                                />
                                <span>{run.completionPercent}% complete</span>
                            </div>
                        </div>
                        <p className="reading-guide">
                            {readingModules[module].guide}
                        </p>
                        <div
                            className={
                                "reading-layout " +
                                (module === "complete-words"
                                    ? "cloze-layout"
                                    : "")
                            }
                        >
                            <section
                                className={
                                    "task-card reading-passage format-" +
                                    step.question.format
                                }
                                aria-label="Reading passage"
                            >
                                <span className="eyebrow">
                                    {step.question.format.replace("-", " ")}
                                </span>
                                <h2 lang="en">{step.question.title}</h2>
                                <div lang="en" className="passage-text">
                                    {module === "complete-words"
                                        ? cloze(step.question.text)
                                        : <PassageText text={step.question.text} targets={vocabularyTargets(step.question.items)} />}
                                </div>
                            </section>
                            {module !== "complete-words" && (
                                <section
                                    className="reading-questions"
                                    aria-label="Questions"
                                >
                                    {step.question.items.map((item) => (
                                        <fieldset
                                            className="reading-question"
                                            key={item.id}
                                            disabled={locked}
                                        >
                                            <legend lang="en">
                                                <span>{item.id}.</span>{" "}
                                                {item.prompt}
                                            </legend>
                                            {item.options.map((option) => (
                                                <label
                                                    key={option.id}
                                                    className={
                                                        "reading-option " +
                                                        (draft.answers[
                                                            item.id
                                                        ] === option.id
                                                            ? "selected"
                                                            : "")
                                                    }
                                                >
                                                    <input
                                                        type="radio"
                                                        name={"item-" + item.id}
                                                        value={option.id}
                                                        checked={
                                                            draft.answers[
                                                                item.id
                                                            ] === option.id
                                                        }
                                                        onChange={() =>
                                                            answer(
                                                                item.id,
                                                                option.id,
                                                            )
                                                        }
                                                    />
                                                    <span className="option-letter">
                                                        {option.id}
                                                    </span>
                                                    <span lang="en">
                                                        {option.text}
                                                    </span>
                                                </label>
                                            ))}
                                        </fieldset>
                                    ))}
                                </section>
                            )}
                        </div>
                        {!stored && !complete && (
                            <div className="info-banner">
                                Your browser could not save your responses. Copy
                                them before leaving.{" "}
                            </div>
                        )}
                        {!complete && (
                            <div className="reading-submit no-print">
                                <span>
                                    {
                                        step.question.items.filter((i) =>
                                            draft.answers[i.id]?.trim(),
                                        ).length
                                    }{" "}
                                    / {step.question.items.length} questions
                                    answered{" "}
                                </span>
                                {draft.attempted && !busy && (
                                    <p>
                                        Retry with the same responses or refresh
                                        to confirm the previous submission.{" "}
                                    </p>
                                )}
                                <button
                                    className="primary"
                                    disabled={busy}
                                    onClick={() =>
                                        step.question.items.some(
                                            (i) => !draft.answers[i.id]?.trim(),
                                        ) && !draft.attempted
                                            ? setConfirm("submit")
                                            : void submit()
                                    }
                                >
                                    {busy
                                        ? "Kontrol ediliyor…"
                                        : draft.attempted
                                          ? "Retry same responses"
                                          : "Check answers"}
                                    <Icon name="arrow" />
                                </button>
                            </div>
                        )}
                        {step.feedback && (
                            <section
                                className="reading-feedback"
                                aria-label="Reading results"
                            >
                                <div className="section-heading">
                                    <div>
                                        <h2 id="reading-feedback" tabIndex={-1}>
                                            Activity complete{" "}
                                        </h2>
                                        <p>
                                            {step.feedback.score} /{" "}
                                            {step.feedback.maxScore} correct ·
                                            Run completion %
                                            {run.completionPercent}
                                        </p>
                                    </div>
                                    <div className="reading-actions no-print"></div>
                                </div>
                                <div className="reading-feedback-grid">
                                    {step.feedback.items.map((item) => (
                                        <article
                                            key={item.id}
                                            className={
                                                "reading-result " +
                                                (item.score
                                                    ? "correct"
                                                    : "incorrect")
                                            }
                                        >
                                            <span className="result-status">
                                                Soru {item.id} ·{" "}
                                                {item.score
                                                    ? "Correct"
                                                    : "Incorrect or unanswered"}
                                            </span>
                                            <h3 lang="en">
                                                {item.correctAnswer}
                                            </h3>
                                            <p>{item.explanation}</p>
                                        </article>
                                    ))}
                                </div>
                                <p className="field-hint">
                                    This practice result is not an official
                                    TOEFL section score. Your selected responses
                                    are not stored in the database.{" "}
                                </p>
                                <div className="reading-actions no-print">
                                    {position < run.totalSteps && (
                                        <button
                                            className="primary"
                                            onClick={() =>
                                                navigate(
                                                    route(
                                                        module,
                                                        run.id,
                                                        position + 1,
                                                    ),
                                                )
                                            }
                                        >
                                            Next activity
                                            <Icon name="arrow" />
                                        </button>
                                    )}
                                    {run.isCompleted && (
                                        <button
                                            className="primary"
                                            disabled={busy}
                                            onClick={() =>
                                                setConfirm("restart")
                                            }
                                        >
                                            Start new run{" "}
                                            <Icon name="refresh" />
                                        </button>
                                    )}
                                    <a className="secondary" href="#/reading">
                                        Back to Reading Studio{" "}
                                    </a>
                                </div>
                            </section>
                        )}
                    </>
                )
            )}
            {confirm && (
                <ConfirmDialog
                    title={
                        confirm === "restart"
                            ? "Start a new run?"
                            : "Submit incomplete responses?"
                    }
                    description={
                        confirm === "restart"
                            ? "Your previous score summary is saved. The new run starts at 0%."
                            : "Unanswered questions count as incorrect and complete this activity."
                    }
                    action={
                        confirm === "restart"
                            ? "Start new run"
                            : "Submit anyway"
                    }
                    onCancel={() => setConfirm(null)}
                    onConfirm={() =>
                        void (confirm === "restart" ? restart() : submit())
                    }
                />
            )}
        </>
    );
}
