import { useEffect, useState } from "react";
import { ApiError, draftKey, loadDraft, post, request, saveDraft } from "./api";
import type { Draft } from "./api";
import {
    ConfirmDialog,
    ErrorBox,
    Icon,
    navigate,
    percent,
    Progress,
    Spinner,
} from "./components";
import { base, modules } from "./types";
import type { Configuration, Module, Run, Step, Submission } from "./types";
import { FeedbackPanel } from "./Report";

/** Manages ordered Writing activities, tab drafts and assessment retries. */
export function Practice({
    module,
    runId,
    position,
    email,
}: {
    module: Module;
    runId: string;
    position: number;
    email: string;
}) {
    const root = base(module) + "/runs/" + runId;
    const key = draftKey(email, runId, position);
    const [draft, setDraft] = useState<Draft>(() => loadDraft(key));
    const [run, setRun] = useState<Run | null>(null);
    const [step, setStep] = useState<Step | null>(null);
    const [configured, setConfigured] = useState(module === "build-sentence");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(true);
    const [busy, setBusy] = useState(false);
    const [revision, reload] = useState(0);
    const [confirm, setConfirm] = useState<"submit" | "restart" | null>(null);
    const [stored, setStored] = useState(true);
    const sentence = module === "build-sentence";
    const current = run?.steps.find((s) => s.position === position);
    const completed = current?.status === "completed";
    const evaluating = current?.status === "evaluating";
    useEffect(() => {
        const controller = new AbortController();
        setLoading(true);
        setError("");
        Promise.all([
            request<Run>(root, { signal: controller.signal }),
            request<Step>(root + "/steps/" + position, {
                signal: controller.signal,
            }),
            sentence
                ? Promise.resolve({ configured: true })
                : request<Configuration>("/api/writing/configuration", {
                      signal: controller.signal,
                  }),
        ])
            .then(([r, s, config]) => {
                setRun(r);
                setStep(s);
                setConfigured(config.configured);
            })
            .catch((e) => {
                if (!controller.signal.aborted) setError(e.message);
            })
            .finally(() => {
                if (!controller.signal.aborted) setLoading(false);
            });
        return () => controller.abort();
    }, [root, position, sentence, revision]);
    useEffect(() => {
        if (!completed) setStored(saveDraft(key, draft));
        else sessionStorage.removeItem(key);
    }, [draft, key, completed]);
    /** Freezes the current response and submits it with the same identifier on retries. */
    async function submit() {
        setConfirm(null);
        setBusy(true);
        setError("");
        const frozen = { ...draft, attempted: true };
        setDraft(frozen);
        saveDraft(key, frozen);
        try {
            const response = await post<Submission>(
                root + "/steps/" + position + "/submissions",
                sentence
                    ? {
                          submissionId: frozen.submissionId,
                          tileIds: frozen.tiles,
                      }
                    : {
                          submissionId: frozen.submissionId,
                          answer: frozen.answer,
                      },
            );
            setRun(response.progress);
            setStep(
                (previous) =>
                    previous && {
                        ...previous,
                        feedback: response.feedback || previous.feedback,
                        report: response.report || previous.report,
                    },
            );
            sessionStorage.removeItem(key);
            setTimeout(
                () => document.getElementById("feedback-heading")?.focus(),
                0,
            );
        } catch (e) {
            setError((e as Error).message);
            if (e instanceof ApiError && [400, 502, 503].includes(e.status))
                setDraft((previous) => ({ ...previous, attempted: false }));
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
            const next = await post<Run>(root + "/restart");
            navigate("/practice/" + module + "/" + next.id + "/1");
        } catch (e) {
            setError((e as Error).message);
        } finally {
            setBusy(false);
        }
    }
    const readonly = busy || completed || draft.attempted || evaluating;
    const question = step?.question;
    /** Appends an available sentence tile to the response order. */
    function addTile(id: number) {
        if (
            !readonly &&
            draft.tiles.length < (question?.slotCount || 0) &&
            !draft.tiles.includes(id)
        )
            setDraft((previous) => ({
                ...previous,
                tiles: [...previous.tiles, id],
            }));
    }
    /** Returns a selected sentence tile to the available tile pool. */
    function removeTile(index: number) {
        if (!readonly)
            setDraft((previous) => ({
                ...previous,
                tiles: previous.tiles.filter((_, i) => i !== index),
            }));
    }
    /** Moves a selected tile within the response while respecting its boundaries. */
    function moveTile(index: number, delta: number) {
        if (
            readonly ||
            index + delta < 0 ||
            index + delta >= draft.tiles.length
        )
            return;
        const tiles = [...draft.tiles];
        [tiles[index], tiles[index + delta]] = [
            tiles[index + delta],
            tiles[index],
        ];
        setDraft((previous) => ({ ...previous, tiles }));
    }
    const wordCount = draft.answer.trim()
        ? draft.answer.trim().split(/\s+/u).length
        : 0;
    const needsConfirmation = sentence
        ? draft.tiles.length < (question?.slotCount || 0)
        : !draft.answer.trim();
    return (
        <>
            <div className="practice-top no-print">
                <button
                    className="text-button"
                    onClick={() => navigate("/writing")}
                >
                    <Icon name="back" size={16} />
                    Back to studio{" "}
                </button>
                <span className="mode-badge">
                    <span className="tiny-dot" />
                    Untimed practice{" "}
                </span>
            </div>
            <div className="practice-heading">
                <div>
                    <span className="eyebrow">{modules[module].caption}</span>
                    <h1 id="page-title" tabIndex={-1}>
                        {modules[module].title}
                    </h1>
                </div>
                {run && (
                    <div className="step-caption">
                        ACTIVITY{" "}
                        <strong>{String(position).padStart(2, "0")}</strong>
                        <span>/ {String(run.totalSteps).padStart(2, "0")}</span>
                    </div>
                )}
            </div>
            {error && (
                <ErrorBox message={error} retry={() => reload(revision + 1)} />
            )}
            {loading ? (
                <Spinner />
            ) : (
                run &&
                step &&
                question && (
                    <div className="practice-layout">
                        <div className="practice-content">
                            <details className="writing-guide no-print">
                                <summary>Task guide · TOEFL 2026</summary>
                                <p>
                                    {sentence
                                        ? "Arrange the words and phrases to make a grammatical sentence or question. Some tiles may be extra. The Writing section contains 10 tasks of this type."
                                        : module === "email"
                                          ? "The test allows 7 minutes for this task. Write an email in complete sentences, address all three points, and use an appropriate tone. Studio practice is untimed."
                                          : "The test allows 10 minutes for this task. Respond to the professor in your own words, supporting your view with reasons or examples. Aim for at least 100 words. Studio practice is untimed."}
                                </p>
                                <p>
                                    Assessment is holistic; word count alone
                                    does not determine deductions. Practice
                                    scores are not official TOEFL section
                                    scores.{" "}
                                </p>
                            </details>
                            <section className="task-card">
                                <div className="card-kicker">
                                    <span className="module-icon small">
                                        <Icon name={modules[module].icon} />
                                    </span>
                                    <span>
                                        {sentence
                                            ? "BUILD A SENTENCE"
                                            : module === "email"
                                              ? "YOUR WRITING TASK"
                                              : "JOIN THE DISCUSSION"}
                                    </span>
                                    <span className="task-number">
                                        0{position}
                                    </span>
                                </div>
                                {sentence ? (
                                    <>
                                        <h2 lang="en">{question.prompt}</h2>
                                        <p className="muted">
                                            Select the tiles in order to fill
                                            the gaps. You may not need every
                                            tile.{" "}
                                        </p>
                                    </>
                                ) : (
                                    <>
                                        <h2 lang="en">{question.title}</h2>
                                        <p
                                            className="task-context preserve"
                                            lang="en"
                                        >
                                            {question.context}
                                        </p>
                                        {question.posts &&
                                            question.posts.length > 0 && (
                                                <div className="discussion-posts">
                                                    {question.posts.map(
                                                        (p, i) => (
                                                            <article
                                                                className="discussion-post"
                                                                key={p.name}
                                                            >
                                                                <div
                                                                    className={
                                                                        "student-avatar tone-" +
                                                                        i
                                                                    }
                                                                >
                                                                    {p.name[0]}
                                                                </div>
                                                                <div>
                                                                    <strong lang="en">
                                                                        {p.name}
                                                                    </strong>
                                                                    <p lang="en">
                                                                        {p.text}
                                                                    </p>
                                                                </div>
                                                            </article>
                                                        ),
                                                    )}
                                                </div>
                                            )}
                                        <div className="requirements">
                                            <h3>Include these points</h3>
                                            <ol lang="en">
                                                {question.requirements?.map(
                                                    (r, i) => (
                                                        <li key={r}>
                                                            <span>{i + 1}</span>
                                                            {r}
                                                        </li>
                                                    ),
                                                )}
                                            </ol>
                                        </div>
                                        <p
                                            className="task-instructions"
                                            lang="en"
                                        >
                                            {question.instructions}
                                        </p>
                                    </>
                                )}
                            </section>
                            {!completed && (
                                <section
                                    className="answer-card no-print"
                                    aria-label="Response area"
                                >
                                    <div className="answer-heading">
                                        <h2>
                                            {sentence
                                                ? "Your sentence"
                                                : "Your response"}
                                        </h2>
                                    </div>
                                    {sentence ? (
                                        <>
                                            <div
                                                className="sentence-builder"
                                                lang="en"
                                                aria-label="Sentence gaps"
                                            >
                                                {(() => {
                                                    let index = 0;
                                                    return question.responseParts?.map(
                                                        (part, partIndex) => {
                                                            if (!part.isSlot)
                                                                return (
                                                                    <span
                                                                        key={
                                                                            partIndex
                                                                        }
                                                                        className="fixed-text"
                                                                    >
                                                                        {
                                                                            part.text
                                                                        }
                                                                    </span>
                                                                );
                                                            const slotIndex =
                                                                index++;
                                                            const tile =
                                                                question.tiles?.find(
                                                                    (t) =>
                                                                        t.id ===
                                                                        draft
                                                                            .tiles[
                                                                            slotIndex
                                                                        ],
                                                                );
                                                            return (
                                                                <button
                                                                    key={
                                                                        partIndex
                                                                    }
                                                                    className={
                                                                        "sentence-slot " +
                                                                        (tile
                                                                            ? "filled"
                                                                            : "")
                                                                    }
                                                                    disabled={
                                                                        readonly ||
                                                                        !tile
                                                                    }
                                                                    onClick={() =>
                                                                        removeTile(
                                                                            slotIndex,
                                                                        )
                                                                    }
                                                                    aria-label={
                                                                        tile
                                                                            ? tile.text +
                                                                              " remove tile"
                                                                            : "Gap " +
                                                                              (slotIndex +
                                                                                  1)
                                                                    }
                                                                >
                                                                    {tile?.text || (
                                                                        <span>
                                                                            {slotIndex +
                                                                                1}
                                                                        </span>
                                                                    )}
                                                                </button>
                                                            );
                                                        },
                                                    );
                                                })()}
                                            </div>
                                            <div
                                                className="tile-bank"
                                                aria-label="Available words"
                                                lang="en"
                                            >
                                                {question.tiles?.map((tile) => (
                                                    <button
                                                        className="word-tile"
                                                        key={tile.id}
                                                        disabled={
                                                            readonly ||
                                                            draft.tiles.includes(
                                                                tile.id,
                                                            ) ||
                                                            draft.tiles
                                                                .length ===
                                                                question.slotCount
                                                        }
                                                        onClick={() =>
                                                            addTile(tile.id)
                                                        }
                                                    >
                                                        {tile.text}
                                                        <span>+</span>
                                                    </button>
                                                ))}
                                            </div>
                                            {draft.tiles.length > 0 && (
                                                <details className="tile-order">
                                                    <summary>
                                                        Reorder tiles{" "}
                                                    </summary>
                                                    <ol>
                                                        {draft.tiles.map(
                                                            (id, i) => (
                                                                <li key={id}>
                                                                    <span lang="en">
                                                                        {
                                                                            question.tiles?.find(
                                                                                (
                                                                                    t,
                                                                                ) =>
                                                                                    t.id ===
                                                                                    id,
                                                                            )
                                                                                ?.text
                                                                        }
                                                                    </span>
                                                                    <button
                                                                        className="icon-button"
                                                                        disabled={
                                                                            readonly ||
                                                                            i ===
                                                                                0
                                                                        }
                                                                        onClick={() =>
                                                                            moveTile(
                                                                                i,
                                                                                -1,
                                                                            )
                                                                        }
                                                                        aria-label={
                                                                            i +
                                                                            1 +
                                                                            ". move tile left"
                                                                        }
                                                                    >
                                                                        ←
                                                                    </button>
                                                                    <button
                                                                        className="icon-button"
                                                                        disabled={
                                                                            readonly ||
                                                                            i ===
                                                                                draft
                                                                                    .tiles
                                                                                    .length -
                                                                                    1
                                                                        }
                                                                        onClick={() =>
                                                                            moveTile(
                                                                                i,
                                                                                1,
                                                                            )
                                                                        }
                                                                        aria-label={
                                                                            i +
                                                                            1 +
                                                                            ". move tile right"
                                                                        }
                                                                    >
                                                                        →
                                                                    </button>
                                                                </li>
                                                            ),
                                                        )}
                                                    </ol>
                                                </details>
                                            )}
                                            <div className="answer-meta">
                                                <span>
                                                    {draft.tiles.length} /{" "}
                                                    {question.slotCount} gaps
                                                    filled{" "}
                                                </span>
                                                <button
                                                    className="text-button"
                                                    disabled={
                                                        readonly ||
                                                        !draft.tiles.length
                                                    }
                                                    onClick={() =>
                                                        setDraft(
                                                            (previous) => ({
                                                                ...previous,
                                                                tiles: [],
                                                            }),
                                                        )
                                                    }
                                                >
                                                    <Icon
                                                        name="refresh"
                                                        size={14}
                                                    />
                                                    Clear{" "}
                                                </button>
                                            </div>
                                        </>
                                    ) : (
                                        <>
                                            <label
                                                className="sr-only"
                                                htmlFor="writing-answer"
                                            >
                                                Your response in English{" "}
                                            </label>
                                            <textarea
                                                id="writing-answer"
                                                className="writing-editor"
                                                lang="en"
                                                spellCheck={false}
                                                autoComplete="off"
                                                placeholder={
                                                    module === "email"
                                                        ? "Dear …,"
                                                        : "In my opinion, …"
                                                }
                                                maxLength={12000}
                                                value={draft.answer}
                                                readOnly={!!readonly}
                                                aria-describedby="answer-hint"
                                                onChange={(e) =>
                                                    setDraft((previous) => ({
                                                        ...previous,
                                                        answer: e.target.value,
                                                    }))
                                                }
                                            />
                                            <div className="answer-meta">
                                                <span>
                                                    <strong>{wordCount}</strong>{" "}
                                                    words{" "}
                                                    {module ===
                                                        "academic-discussion" && (
                                                        <span className="word-target">
                                                            {" "}
                                                            · Target: 100+ words
                                                        </span>
                                                    )}
                                                </span>
                                                <span>
                                                    {draft.answer.length.toLocaleString(
                                                        "en",
                                                    )}{" "}
                                                    / 12.000 karakter
                                                </span>
                                            </div>
                                            <p
                                                id="answer-hint"
                                                className="field-hint"
                                            >
                                                Write in English using your own
                                                words. Length alone does not
                                                determine your score.{" "}
                                            </p>
                                        </>
                                    )}
                                    {!stored && (
                                        <div className="info-banner">
                                            Your browser could not save the
                                            draft. Copy your response before
                                            leaving.{" "}
                                        </div>
                                    )}
                                    {draft.attempted &&
                                        !busy &&
                                        !evaluating && (
                                            <p className="field-hint">
                                                Your submitted response is
                                                preserved. Retry the same
                                                response or refresh its
                                                status.{" "}
                                            </p>
                                        )}
                                    {!configured && !sentence && (
                                        <div className="info-banner">
                                            <Icon name="spark" />
                                            <p>
                                                AI assessment is currently
                                                unavailable. You can prepare a
                                                draft and submit when assessment
                                                becomes available.{" "}
                                            </p>
                                        </div>
                                    )}
                                    {busy && (
                                        <Spinner text="Assessing your response. This may take up to two minutes…" />
                                    )}
                                    {evaluating && !busy && (
                                        <div className="info-banner">
                                            <p>
                                                Your response is still being
                                                assessed. Refresh its status
                                                shortly.{" "}
                                            </p>
                                            <button
                                                className="secondary"
                                                onClick={() =>
                                                    reload(revision + 1)
                                                }
                                            >
                                                Durumu yenile
                                            </button>
                                        </div>
                                    )}
                                    <div className="submit-row">
                                        <span>
                                            <Icon name="lock" size={14} />
                                            Submitting finalizes this
                                            response.{" "}
                                        </span>
                                        <button
                                            className="primary"
                                            disabled={
                                                busy ||
                                                !configured ||
                                                evaluating
                                            }
                                            onClick={() =>
                                                needsConfirmation
                                                    ? setConfirm("submit")
                                                    : void submit()
                                            }
                                        >
                                            {busy
                                                ? "Assessing…"
                                                : draft.attempted
                                                  ? "Retry same response"
                                                  : sentence
                                                    ? "Check answer"
                                                    : "Assess my response"}
                                            <Icon
                                                name={
                                                    sentence ? "check" : "spark"
                                                }
                                                size={18}
                                            />
                                        </button>
                                    </div>
                                </section>
                            )}
                            {completed && (
                                <FeedbackPanel
                                    module={module}
                                    run={run}
                                    position={position}
                                    step={step}
                                />
                            )}
                            {completed && (
                                <div className="next-step no-print">
                                    {run.isCompleted ? (
                                        <div className="completion-card">
                                            <span className="completion-symbol">
                                                <Icon name="check" size={24} />
                                            </span>
                                            <div>
                                                <h2>Run complete.</h2>
                                                <p>
                                                    {run.totalSteps} activities
                                                    completed. You can start a
                                                    new practice run.{" "}
                                                </p>
                                            </div>
                                            <button
                                                className="primary"
                                                disabled={busy}
                                                onClick={() =>
                                                    setConfirm("restart")
                                                }
                                            >
                                                <Icon
                                                    name="refresh"
                                                    size={16}
                                                />
                                                Start new run{" "}
                                            </button>
                                        </div>
                                    ) : (
                                        <button
                                            className="primary"
                                            onClick={() =>
                                                navigate(
                                                    "/practice/" +
                                                        module +
                                                        "/" +
                                                        runId +
                                                        "/" +
                                                        run.nextPosition,
                                                )
                                            }
                                        >
                                            Next activity{" "}
                                            <Icon name="arrow" size={18} />
                                        </button>
                                    )}
                                </div>
                            )}
                        </div>
                        <aside className="practice-aside no-print">
                            <section className="path-card">
                                <div className="path-title">
                                    <h2>Your learning path</h2>
                                    <span>
                                        {percent(run.completionPercent)}%
                                    </span>
                                </div>
                                <Progress
                                    value={run.completionPercent}
                                    label="Run completion"
                                />
                                <p className="field-hint">
                                    {run.completedSteps} / {run.totalSteps}{" "}
                                    activities completed{" "}
                                </p>
                                <ol className="path-steps">
                                    {run.steps.map((s) => (
                                        <li key={s.position}>
                                            <button
                                                className={
                                                    (position === s.position
                                                        ? "current "
                                                        : "") + s.status
                                                }
                                                aria-current={
                                                    position === s.position
                                                        ? "step"
                                                        : undefined
                                                }
                                                aria-label={
                                                    "Activity " +
                                                    String(s.position).padStart(
                                                        2,
                                                        "0",
                                                    ) +
                                                    (s.status === "locked"
                                                        ? ", locked"
                                                        : s.status ===
                                                            "completed"
                                                          ? ", completed"
                                                          : "")
                                                }
                                                disabled={
                                                    s.status === "locked" ||
                                                    busy
                                                }
                                                onClick={() =>
                                                    navigate(
                                                        "/practice/" +
                                                            module +
                                                            "/" +
                                                            runId +
                                                            "/" +
                                                            s.position,
                                                    )
                                                }
                                            >
                                                <span className="path-step-icon">
                                                    {s.status ===
                                                    "completed" ? (
                                                        <Icon
                                                            name="check"
                                                            size={15}
                                                        />
                                                    ) : s.status ===
                                                      "locked" ? (
                                                        <Icon
                                                            name="lock"
                                                            size={13}
                                                        />
                                                    ) : (
                                                        String(
                                                            s.position,
                                                        ).padStart(2, "0")
                                                    )}
                                                </span>
                                                <span>
                                                    Activity{" "}
                                                    {String(
                                                        s.position,
                                                    ).padStart(2, "0")}
                                                    <small>
                                                        {s.status ===
                                                        "completed"
                                                            ? "Completed"
                                                            : s.status ===
                                                                "evaluating"
                                                              ? "Assessing"
                                                              : s.status ===
                                                                  "locked"
                                                                ? "Locked"
                                                                : "Ready to practice"}
                                                    </small>
                                                </span>
                                                {position === s.position && (
                                                    <span className="current-dot" />
                                                )}
                                            </button>
                                        </li>
                                    ))}
                                </ol>
                            </section>
                            <div className="coach-note">
                                <Icon name="spark" />
                                <h3>
                                    {sentence
                                        ? "Start with the sentence structure."
                                        : "Start with a clear idea."}
                                </h3>
                                <p>
                                    {sentence
                                        ? "Identify the subject and verb. Consider tense and how the words relate to one another. A small change can alter the meaning."
                                        : "State your purpose first, then add a reason or example. Read your response once more before submitting."}
                                </p>
                            </div>
                            <p className="privacy-note">
                                Responses are not saved in the database. Drafts
                                in this tab are cleared when you sign out.{" "}
                            </p>
                        </aside>
                    </div>
                )
            )}
            {confirm && (
                <ConfirmDialog
                    onCancel={() => setConfirm(null)}
                    onConfirm={() =>
                        void (confirm === "restart" ? restart() : submit())
                    }
                    title={
                        confirm === "restart"
                            ? "Start a new run?"
                            : "Submit this response?"
                    }
                    description={
                        confirm === "restart"
                            ? "Your previous progress remains saved. The current learning path starts again at 0%."
                            : sentence
                              ? "Some gaps are empty. An incomplete answer may receive 0 points and complete this activity."
                              : "An empty response receives 0 points and completes this activity."
                    }
                    action={
                        confirm === "restart"
                            ? "Start new run"
                            : "Submit anyway"
                    }
                />
            )}
        </>
    );
}
