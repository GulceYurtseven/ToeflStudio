import { FormatHeading } from "./Community";
import { useEffect, useState } from "react";
import { request } from "./api";
import { ErrorBox } from "./components";

type Format = {
    approximateBaseMinutes: number;
    scoredItemCount: number;
    possibleUnscoredItems: number;
    maxRawScore: number;
    source: string;
};

/** Loads official format metadata and explains the differences in local practice mode. */
export function TestFormat({
    section,
}: {
    section: "reading" | "writing" | "listening";
}) {
    const [format, setFormat] = useState<Format | null>(null);
    const [error, setError] = useState("");
    const [revision, reload] = useState(0);
    useEffect(() => {
        const controller = new AbortController();
        setFormat(null);
        setError("");
        request<Format>("/api/test-format/" + section, {
            signal: controller.signal,
        })
            .then(setFormat)
            .catch((e) => {
                if (!controller.signal.aborted) setError(e.message);
            });
        return () => controller.abort();
    }, [section, revision]);
    return (
        <details className="format-guide no-print">
            <FormatHeading />
            {error && (
                <ErrorBox
                    message={
                        section === "listening"
                            ? "Test format could not be loaded."
                            : error
                    }
                    retryLabel={
                        section === "listening" ? "Try again" : "Try again"
                    }
                    retry={() => reload(revision + 1)}
                />
            )}
            {!format && !error && (
                <p role="status">
                    {section === "listening"
                        ? "Loading test format…"
                        : "Loading test format…"}
                </p>
            )}
            {format && section === "listening" ? (
                <div lang="en">
                    <p>
                        The approximate base time is{" "}
                        {format.approximateBaseMinutes} minutes, excluding
                        directions. The actual duration may vary.
                    </p>
                    <p>
                        The first stage contains 20 scored questions.
                        Performance determines the second stage, which contains
                        15 scored questions. Up to{" "}
                        {format.possibleUnscoredItems} additional unscored
                        questions may be included.
                    </p>
                    <p>
                        On the actual test, you move forward and cannot return
                        to a previous question. Exact per-question timers and
                        routing thresholds are not published in the technical
                        manual.
                    </p>
                    <p>
                        Studio activities and recommended channels support
                        practice. They are not a timed adaptive exam and do not
                        produce an official 1–6 section score.
                    </p>
                    <a href={format.source} target="_blank" rel="noreferrer">
                        ETS technical manual
                    </a>
                </div>
            ) : (
                format && (
                    <>
                        <p>
                            The approximate base time for this section is{" "}
                            {format.approximateBaseMinutes} minutes, excluding
                            directions.{" "}
                        </p>
                        {section === "writing" ? (
                            <>
                                <p>
                                    10 Build a Sentence tasks, one Email, and
                                    one Academic Discussion. Sentences earn 0–1
                                    points; the two written responses earn 0–5
                                    each. Maximum raw score: 20.{" "}
                                </p>
                                <p>
                                    Email allows 7 minutes and Discussion 10
                                    minutes. The technical manual does not
                                    specify an exact shared timer for Build a
                                    Sentence.{" "}
                                </p>
                            </>
                        ) : (
                            <>
                                <p>
                                    The first stage has 20 scored questions;
                                    performance determines the second stage with
                                    15 scored questions.{" "}
                                    {format.possibleUnscoredItems} unscored
                                    questions may also appear; totals and
                                    duration can vary.{" "}
                                </p>
                                <p>
                                    {section === "reading"
                                        ? "Reading permits Next and Back within the current module. All ten Complete the Words gaps appear together. Completed modules are locked."
                                        : "Listening moves forward without returning to previous questions. Exact per-question timers are not published in the manual."}
                                </p>
                            </>
                        )}
                        <p>
                            Studios support self-paced practice. The separate
                            Practice Exams apply section-specific navigation and
                            timers. Raw scores are not converted to official 1–6
                            section scores.{" "}
                        </p>
                        <a
                            href={format.source}
                            target="_blank"
                            rel="noreferrer"
                        >
                            ETS technical manual{" "}
                        </a>
                    </>
                )
            )}
        </details>
    );
}
