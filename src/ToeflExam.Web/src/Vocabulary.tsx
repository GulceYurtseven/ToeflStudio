import { useEffect, useMemo, useState } from "react";
import { request } from "./api";
import { ErrorBox, Icon, Spinner } from "./components";
import { Notebook, RecommendedStudyChannels } from "./Community";
import "./vocabulary.css";

export type VocabularyWord = {
    id: string;
    word: string;
    partOfSpeech: string;
    meaning: string;
    exampleSentence: string;
    sourceIds: string[];
    aliases: string[];
};
type Catalog = {
    version: string;
    words: VocabularyWord[];
    sources: {
        id: string;
        title: string;
        url: string | null;
        coverage: string;
        note: string;
        wordIds: string[];
    }[];
};

/** Keeps the complete word bank and answer-reveal practice together on one studio page. */
export function VocabularyStudio() {
    const [catalog, setCatalog] = useState<Catalog | null>(null);
    const [error, setError] = useState("");
    const [revision, reload] = useState(0);
    const [query, setQuery] = useState("");
    const [selected, setSelected] = useState<VocabularyWord | null>(null);
    const [revealed, setRevealed] = useState(false);

    // Load the complete curated bank once; search and flashcards need no navigation or further API calls.
    useEffect(() => {
        const controller = new AbortController();
        setError("");
        void request<Catalog>("/api/vocabulary", { signal: controller.signal })
            .then((data) => {
                if (!controller.signal.aborted) setCatalog(data);
            })
            .catch((e: Error) => {
                if (!controller.signal.aborted) setError(e.message);
            });
        return () => controller.abort();
    }, [revision]);

    const visibleWords = useMemo(() => {
        const term = query.trim().toLocaleLowerCase("en");
        return (catalog?.words ?? []).filter((word) =>
            `${word.word} ${word.aliases.join(" ")} ${word.meaning} ${word.exampleSentence}`
                .toLocaleLowerCase("en")
                .includes(term),
        );
    }, [catalog, query]);

    /** Draws from the entire bank, avoiding an immediate repeat and hiding the previous answer. */
    function getRandomWord() {
        const candidates = (catalog?.words ?? []).filter(
            (word) => word.id !== selected?.id,
        );
        const pool = candidates.length ? candidates : (catalog?.words ?? []);
        if (!pool.length) return;
        setSelected(pool[Math.floor(Math.random() * pool.length)]);
        setRevealed(false);
    }

    return (
        <section className="studio-dashboard vocabulary-studio">
            <div className="page-heading">
                <div>
                    <span className="eyebrow">TOEFL VOCABULARY PRACTICE</span>
                    <h1 id="page-title" tabIndex={-1}>
                        Vocabulary <em>Studio</em>
                    </h1>
                    <p>
                        Learn the word. Understand the meaning. Make it yours.
                    </p>
                </div>
                <span className="mode-badge">Self-paced practice</span>
            </div>

            <section
                className="vocabulary-flashcard"
                aria-labelledby="flashcard-title"
            >
                <div className="vocabulary-flashcard-heading">
                    <div className="vocabulary-title-group">
                        <Notebook compact />
                        <div>
                            <span className="eyebrow">
                                A LITTLE RECALL, EVERY DAY
                            </span>
                            <h2 id="flashcard-title">One word at a time</h2>
                        </div>
                    </div>
                    <button
                        className="primary"
                        onClick={getRandomWord}
                        disabled={!catalog?.words.length}
                    >
                        <Icon name="refresh" size={17} /> Get Random Word
                    </button>
                </div>
                <p className="vocabulary-prompt">
                    Think of the meaning before revealing the answer.
                </p>
                <div
                    className="vocabulary-card-face"
                    aria-live="polite"
                    aria-atomic="true"
                >
                    {selected ? (
                        <>
                            <span className="eyebrow">WORD</span>
                            <h3 className="vocabulary-selected-word">
                                {selected.word}
                            </h3>
                        </>
                    ) : (
                        <div className="vocabulary-card-placeholder">
                            <Icon name="book" size={38} />
                            <p>Your next word is waiting.</p>
                            <span>Choose Get Random Word to begin.</span>
                        </div>
                    )}
                </div>
                {selected && (
                    <div className="vocabulary-reveal">
                        <button
                            className="secondary"
                            aria-expanded={revealed}
                            aria-controls="vocabulary-answer"
                            disabled={revealed}
                            onClick={() => setRevealed(true)}
                        >
                            {revealed ? "Answer shown" : "Show Answer"}
                        </button>
                        <div
                            id="vocabulary-answer"
                            role="region"
                            aria-label="Flashcard answer"
                            aria-live="polite"
                        >
                            {revealed && (
                                <div
                                    className="vocabulary-answer-content"
                                    key={selected.id}
                                >
                                    <WordAnswer word={selected} />
                                </div>
                            )}
                        </div>
                    </div>
                )}
            </section>

            {error && (
                <ErrorBox
                    message={error}
                    retry={() => reload((value) => value + 1)}
                />
            )}
            {!catalog && !error && <Spinner text="Loading the word bank…" />}
            {catalog && (
                <>
                    <section
                        className="vocabulary-library"
                        aria-labelledby="word-bank-title"
                    >
                        <div className="vocabulary-library-heading">
                            <div>
                                <span className="eyebrow">EXPLORE & LEARN</span>
                                <h2 id="word-bank-title">
                                    Your word bank{" "}
                                    <span className="vocabulary-count">
                                        {catalog.words.length}
                                    </span>
                                </h2>
                            </div>
                            <div className="vocabulary-search">
                                <label htmlFor="vocabulary-search">
                                    Search words, meanings, or examples
                                </label>
                                <input
                                    id="vocabulary-search"
                                    type="search"
                                    placeholder="Find a word…"
                                    maxLength={200}
                                    value={query}
                                    onChange={(e) => setQuery(e.target.value)}
                                    autoComplete="off"
                                />
                            </div>
                        </div>
                        <p className="vocabulary-results" role="status">
                            {visibleWords.length} of {catalog.words.length}{" "}
                            words · A–Z
                        </p>
                        {visibleWords.length === 0 ? (
                            <div className="empty-state">
                                <h3>No matching words</h3>
                                <p>
                                    Try a different word or clear your search to
                                    see the full bank.
                                </p>
                                <button
                                    className="secondary"
                                    onClick={() => setQuery("")}
                                >
                                    Clear search
                                </button>
                            </div>
                        ) : (
                            <ul
                                className="vocabulary-grid"
                                aria-label="Vocabulary word bank"
                            >
                                {visibleWords.map((word) => (
                                    <li
                                        className="vocabulary-word-card"
                                        key={word.id}
                                    >
                                        <div className="vocabulary-word-heading">
                                            <div>
                                                <span className="eyebrow">
                                                    WORD
                                                </span>
                                                <h3>{word.word}</h3>
                                            </div>
                                            <span className="vocabulary-part-of-speech">
                                                {word.partOfSpeech}
                                            </span>
                                        </div>
                                        <WordAnswer word={word} />
                                    </li>
                                ))}
                            </ul>
                        )}
                    </section>
                    <details className="vocabulary-sources">
                        <summary>Learning sources</summary>
                        <p>
                            Words and expressions from the supplied learning
                            resources, with definitions and examples
                            written for this studio. Shared words appear once.
                        </p>
                        <ul>
                            {catalog.sources.map((source) => (
                                <li key={source.id}>
                                    {source.url ? (
                                        <a
                                            href={source.url}
                                            target="_blank"
                                            rel="noopener noreferrer"
                                        >
                                            {source.title}
                                            <span className="sr-only">
                                                {" "}
                                                (opens in a new tab)
                                            </span>
                                        </a>
                                    ) : (
                                        <strong>{source.title}</strong>
                                    )}
                                    <span>
                                        {source.wordIds.length} words and
                                        expressions
                                    </span>
                                    {source.coverage !== "complete" && (
                                        <p>{source.note}</p>
                                    )}
                                </li>
                            ))}
                        </ul>
                    </details>
                </>
            )}
            <RecommendedStudyChannels />
        </section>
    );
}

/** Renders learning content as text so neither source material nor search input can inject markup. */
function WordAnswer({ word }: { word: VocabularyWord }) {
    return (
        <dl className="vocabulary-definition">
            <dt>Meaning</dt>
            <dd>{word.meaning}</dd>
            <dt>Example Sentence</dt>
            <dd className="vocabulary-example">{word.exampleSentence}</dd>
        </dl>
    );
}
