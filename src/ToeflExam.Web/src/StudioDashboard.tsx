import { FormatHeading, RecommendedStudyChannels } from "./Community";
import { useEffect, useState } from "react";
import { request, post } from "./api";
import { Icon, Progress, ErrorBox, Spinner, navigate } from "./components";
import { modules as writing, base } from "./types";
import { readingModules as reading } from "./Reading";
import { TestFormat } from "./TestFormat";
import { BankDashboard } from "./BankPractice";
import "./studio.css";
type Section = "writing" | "reading" | "speaking" | "listening";
type Module = {
    module: string;
    title: string;
    totalSteps: number;
    completedSteps: number;
    completionPercent: number;
};
const speaking = {
    "listen-repeat": {
        title: "Listen and Repeat",
        description:
            "Repeat each sentence and review content accuracy after transcription.",
        icon: "mic",
    },
    interview: {
        title: "Take an Interview",
        description:
            "Develop your ideas and answer questions about experiences and opinions.",
        icon: "chat",
    },
};
/** Opens Writing practice through the common Studio dashboard. */
export function WritingStudio() {
    return <StudioDashboard section="writing" />;
}
/** Shares the format guide, ordered paths, progress and study channels across skills. */
export function StudioDashboard({ section }: { section: Section }) {
    const [modules, setModules] = useState<Module[] | null>(null),
        [error, setError] = useState(""),
        [busy, setBusy] = useState(false),
        [revision, reload] = useState(0),
        [configured, setConfigured] = useState(true);
    const title = section[0].toUpperCase() + section.slice(1);
    useEffect(() => {
        if (section === "listening") return;
        const c = new AbortController();
        setModules(null);
        setError("");
        void (async () => {
            try {
                const result = await request<{
                    modules: Module[];
                    configured?: boolean;
                }>(`/api/${section}`, { signal: c.signal });
                setModules(result.modules);
                setConfigured(result.configured !== false);
                if (section === "writing")
                    setConfigured(
                        (
                            await request<{ configured: boolean }>(
                                "/api/writing/configuration",
                                { signal: c.signal },
                            )
                        ).configured,
                    );
            } catch (e) {
                if (!c.signal.aborted) setError((e as Error).message);
            }
        })();
        return () => c.abort();
    }, [section, revision]);
    const completed = modules?.reduce((n, m) => n + m.completedSteps, 0) ?? 0,
        total = modules?.reduce((n, m) => n + m.totalSteps, 0) ?? 0;
    /** Starts or resumes the selected learning path and navigates to its accessible activity. */
    async function start(module: string) {
        setBusy(true);
        setError("");
        try {
            const endpoint =
                section === "writing"
                    ? base(module as keyof typeof writing)
                    : section === "reading"
                      ? `/api/reading/${module}`
                      : `/api/practice/speaking/${module}`;
            const run = await post<{
                id: string;
                nextPosition: number | null;
                totalSteps: number;
            }>(endpoint + "/runs");
            navigate(
                `/${section === "writing" ? "practice" : section}/${module}/${run.id}/${run.nextPosition ?? run.totalSteps}`,
            );
        } catch (e) {
            setError((e as Error).message);
        } finally {
            setBusy(false);
        }
    }
    const definitions: Record<
        string,
        { title: string; description: string; icon: string }
    > =
        section === "writing"
            ? writing
            : section === "reading"
              ? reading
              : speaking;
    return (
        <section className="studio-dashboard">
            <div className="page-heading">
                <div>
                    <span className="eyebrow">TOEFL SKILL PRACTICE</span>
                    <h1 id="page-title" tabIndex={-1}>
                        {title} <em>Studio</em>
                    </h1>
                    <p>
                        Choose a learning path, practice in order, and track
                        your progress.
                    </p>
                </div>
                <span className="mode-badge">
                    {section === "speaking"
                        ? "Timed responses"
                        : "Self-paced practice"}
                </span>
            </div>
            {section !== "speaking" && <TestFormat section={section} />}
            {section === "speaking" && (
                <details className="format-guide">
                    <FormatHeading />
                    <p>
                        Use headphones and a microphone. Recording starts after
                        the prompt and stops automatically. In Studio practice,
                        review and re-record before submitting.
                    </p>
                    <p>
                        Audio is transcribed first. AI feedback assesses
                        transcript content and language, not pronunciation or
                        vocal fluency. Recordings are not saved to server files
                        or databases. Detailed feedback is temporary.
                    </p>
                </details>
            )}
            {error && (
                <ErrorBox message={error} retry={() => reload(revision + 1)} />
            )}
            {section === "listening" ? (
                <BankDashboard mode="listening" />
            ) : (
                <>
                    {!modules && !error && <Spinner />}
                    {modules && (
                        <>
                            <section
                                className="studio-overview"
                                aria-label={`${title} progress`}
                            >
                                <span className="eyebrow">YOUR PROGRESS</span>
                                <h2>
                                    {completed} of {total} activities completed
                                </h2>
                                <p>
                                    Finish a path to start a new run. Completion
                                    is separate from accuracy.
                                </p>
                                <Progress
                                    value={
                                        total ? (completed / total) * 100 : 0
                                    }
                                    label={`${title} overall completion`}
                                />
                            </section>
                            <div className="module-grid">
                                {modules.map((m, i) => (
                                    <article
                                        className={`module-card module-${i % 3}`}
                                        key={m.module}
                                    >
                                        <span className="module-icon">
                                            <Icon
                                                name={
                                                    definitions[m.module]
                                                        ?.icon ?? "book"
                                                }
                                                size={28}
                                            />
                                        </span>
                                        <h2>
                                            {definitions[m.module]?.title ??
                                                m.title}
                                        </h2>
                                        <p>
                                            {definitions[m.module]?.description}
                                        </p>
                                        <div className="module-stats">
                                            <span>
                                                {m.completedSteps} /{" "}
                                                {m.totalSteps} activities
                                            </span>
                                            <strong>
                                                {m.completionPercent}%
                                            </strong>
                                        </div>
                                        <Progress
                                            value={m.completionPercent}
                                            label={`${m.title} completion`}
                                        />
                                        <button
                                            className="primary full"
                                            disabled={busy}
                                            onClick={() => void start(m.module)}
                                        >
                                            {busy
                                                ? "Opening…"
                                                : m.completionPercent === 100
                                                  ? "Review completed run"
                                                  : m.completedSteps
                                                    ? "Continue practice"
                                                    : "Start practice"}
                                            <Icon name="arrow" />
                                        </button>
                                    </article>
                                ))}
                            </div>
                        </>
                    )}
                    {!configured && (
                        <div className="config-notice">
                            AI assessment is currently unavailable. You can
                            prepare responses and continue objective practice.
                        </div>
                    )}
                </>
            )}
            <RecommendedStudyChannels />
        </section>
    );
}
