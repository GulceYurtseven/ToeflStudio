import { Notebook, RecommendedStudyChannels } from "./Community";
import { Icon } from "./components";

/** Presents skill studios, vocabulary practice, exams and optional study resources. */
export function Studios() {
    return (
        <>
            <div className="studio-hero">
                <span className="eyebrow">TOEFL · YOUR WORKSPACE</span>
                <h1 id="page-title" tabIndex={-1}>
                    Which skill will you <em>practice today?</em>
                </h1>
                <p>Choose a studio and continue your learning.</p>
                <div className="hero-notebook" aria-hidden="true">
                    <Notebook />
                </div>
            </div>
            <div className="studio-grid">
                <a href="#/exams" className="studio-card studio-writing">
                    <span className="studio-symbol">
                        <Icon name="history" size={36} />
                    </span>
                    <span className="studio-kicker">
                        FOUR SKILLS · TIMED PRACTICE{" "}
                    </span>
                    <h2>TOEFL Practice Exams</h2>
                    <p>
                        Three exams with questions separate from Studio
                        practice. Save your detailed report at the end.{" "}
                    </p>
                    <span className="studio-link">
                        3 practice exams <Icon name="arrow" />
                    </span>
                </a>
                <a href="#/speaking" className="studio-card studio-speaking">
                    <span className="studio-symbol">
                        <Icon name="mic" size={36} />
                    </span>
                    <span className="studio-kicker">
                        LISTEN · SPEAK · IMPROVE{" "}
                    </span>
                    <h2>Speaking Studio</h2>
                    <p>
                        Repeat sentences and share your views. Review detailed
                        feedback on your responses.{" "}
                    </p>
                    <span className="studio-link">
                        2 learning paths <Icon name="arrow" />
                    </span>
                </a>
                <a href="#/reading" className="studio-card studio-reading">
                    <span className="studio-symbol">
                        <Icon name="book" size={36} />
                    </span>
                    <span className="studio-kicker">
                        READ · UNDERSTAND · EXPLORE
                    </span>
                    <h2>Reading Studio</h2>
                    <p>
                        Complete missing words and analyze everyday and academic
                        texts.{" "}
                    </p>
                    <span className="studio-link">
                        3 learning paths <Icon name="arrow" />
                    </span>
                </a>
                <a href="#/listening" className="studio-card studio-listening">
                    <span className="studio-symbol">
                        <Icon name="headphones" size={36} />
                    </span>
                    <span className="studio-kicker">
                        LISTEN · UNDERSTAND · RESPOND
                    </span>
                    <h2>Listening Studio</h2>
                    <p>
                        Practice conversations, announcements, and academic
                        talks.{" "}
                    </p>
                    <span className="studio-link">
                        4 learning paths <Icon name="arrow" />
                    </span>
                </a>
                <a href="#/writing" className="studio-card studio-writing">
                    <span className="studio-symbol">
                        <Icon name="pen" size={36} />
                    </span>
                    <span className="studio-kicker">
                        BUILD · WRITE · EXPRESS
                    </span>
                    <h2>Writing Studio</h2>
                    <p>
                        Build sentences and get feedback on emails and academic
                        discussion responses.{" "}
                    </p>
                    <span className="studio-link">
                        3 learning paths <Icon name="arrow" />
                    </span>
                </a>
                <a
                    href="#/vocabulary"
                    className="studio-card studio-vocabulary"
                >
                    <span className="studio-symbol">
                        <Icon name="book" size={36} />
                    </span>
                    <span className="studio-kicker">
                        DISCOVER · RECALL · REMEMBER
                    </span>
                    <h2>Vocabulary Studio</h2>
                    <p>
                        Explore meanings and examples, then put your memory to
                        work with a random word.
                    </p>
                    <span className="studio-link">
                        Word bank & flashcards <Icon name="arrow" />
                    </span>
                </a>
            </div>
            <RecommendedStudyChannels />
        </>
    );
}
