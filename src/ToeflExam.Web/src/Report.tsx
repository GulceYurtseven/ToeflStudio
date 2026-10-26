import { Icon } from "./components";
import type { Module, Run, Step } from "./types";

const categoryLabels: Record<string, string> = {
    task: "Task fulfillment",
    organization: "Organization",
    grammar: "Grammar",
    vocabulary: "Vocabulary",
    register: "Register",
    mechanics: "Spelling and punctuation",
};
/** Displays rubric scores and explanations without exposing a studio download action. */
export function FeedbackPanel({
    module,
    run,
    position,
    step,
}: {
    module: Module;
    run: Run;
    position: number;
    step: Step;
}) {
    const sentence = module === "build-sentence";
    const score = sentence
        ? step.feedback?.rawScore
        : (step.report?.feedback.score ??
          run.steps.find((s) => s.position === position)?.score);
    const report = step.report;
    const feedback = report?.feedback;
    return (
        <section className="feedback-card">
            <div className="feedback-title">
                <span className="module-icon">
                    <Icon name={sentence ? "check" : "spark"} size={24} />
                </span>
                <div>
                    <span className="eyebrow">
                        {sentence
                            ? "RESPONSE ASSESSED"
                            : "REVIEW YOUR RESPONSE"}
                    </span>
                    <h2 id="feedback-heading" tabIndex={-1}>
                        {sentence
                            ? score === 1
                                ? "You built a correct sentence."
                                : "Review the correct structure."
                            : "Your feedback is ready."}
                    </h2>
                </div>
                <div className="score-box">
                    <strong>{score ?? "—"}</strong>
                    <span>/ {sentence ? 1 : 5}</span>
                </div>
            </div>
            <p className="score-caption">
                {sentence ? "Raw question score" : "AI practice estimate"} · Not
                an official TOEFL section score.{" "}
            </p>
            {sentence && step.feedback && (
                <>
                    <div className="correct-answer">
                        <span className="eyebrow">CORRECT SENTENCE</span>
                        {step.feedback.correctSentences.map((text) => (
                            <p lang="en" key={text}>
                                {text}
                            </p>
                        ))}
                    </div>
                    <div className="feedback-section">
                        <h3>Why this works</h3>
                        <p>{step.feedback.explanation}</p>
                    </div>
                </>
            )}
            {!sentence && !report && (
                <div className="info-banner">
                    <p>
                        Your score is saved, but detailed feedback is no longer
                        available. Detailed feedback is temporary.{" "}
                    </p>
                </div>
            )}
            {feedback && report && (
                <>
                    <div className="feedback-section">
                        <h3>Why you received this score</h3>
                        <p className="preserve">{feedback.rationale}</p>
                    </div>
                    {feedback.strengths.length > 0 && (
                        <div className="strengths">
                            <h3>
                                <Icon name="check" size={18} />
                                Strengths{" "}
                            </h3>
                            <ul>
                                {feedback.strengths.map((text, i) => (
                                    <li key={i}>{text}</li>
                                ))}
                            </ul>
                        </div>
                    )}
                    {feedback.issues.length > 0 && (
                        <div className="feedback-section">
                            <h3>Areas to improve</h3>
                            <div className="issues">
                                {feedback.issues.map((issue, i) => (
                                    <article className="issue" key={i}>
                                        <span className="issue-category">
                                            {categoryLabels[issue.category] ||
                                                issue.category}
                                        </span>
                                        {issue.evidence && (
                                            <blockquote lang="en">
                                                {issue.evidence}
                                            </blockquote>
                                        )}
                                        <p>{issue.explanation}</p>
                                        <div className="suggestion">
                                            <Icon name="arrow" size={16} />
                                            <p className="preserve">
                                                {issue.suggestion}
                                            </p>
                                        </div>
                                    </article>
                                ))}
                            </div>
                        </div>
                    )}
                    <div className="feedback-section">
                        <h3>Next steps</h3>
                        <ol className="next-tips">
                            {feedback.nextSteps.map((text, i) => (
                                <li key={i}>{text}</li>
                            ))}
                        </ol>
                    </div>
                    <details className="response-details">
                        <summary>
                            Your submitted response{" "}
                            <span>{report.wordCount} words</span>
                        </summary>
                        <div className="preserve" lang="en">
                            {report.answer || "(Empty response)"}
                        </div>
                    </details>
                    {feedback.improvedResponse && (
                        <details className="response-details">
                            <summary>Review an improved example</summary>
                            <div className="preserve" lang="en">
                                {feedback.improvedResponse}
                            </div>
                        </details>
                    )}
                </>
            )}
        </section>
    );
}
