import { useEffect, useRef, useState } from "react";
import type { FormEvent } from "react";
import { getSession, csrfPost } from "./api";
import { ErrorBox, Icon } from "./components";

/** Reuses the login notebook illustration at full size or as a compact format icon. */
export function Notebook({ compact = false }: { compact?: boolean }) {
    return (
        <div
            className={compact ? "notebook-mini" : "auth-paper"}
            aria-hidden="true"
        >
            <div className="paper-label">
                <Icon name="pen" size={compact ? 12 : 18} />
                {!compact && " YOUR TOEFL PREPARATION"}
            </div>
            {!compact && (
                <div className="paper-sentence">
                    Read. Listen.
                    <br />
                    Write. <span>Speak.</span>
                </div>
            )}
            <div className="paper-lines" />
            <span className="paper-mark">
                <Icon name="check" size={compact ? 12 : 24} />
            </span>
        </div>
    );
}

/** Provides one accessible visual treatment for all studio format disclosures. */
export function FormatHeading() {
    return (
        <summary>
            <Notebook compact />
            <span>2026 Test Format</span>
            <span className="format-chevron" aria-hidden="true">
                ⌄
            </span>
        </summary>
    );
}

const channels = [
    "TOEFLofficial",
    "TSTPrep",
    "TOEFLTUTOR",
    "gabbleai",
    "InspireSpeakEasy",
    "linguamarina",
    "LearnEnglishWithTVSeries",
];

/** Shares optional external resources without embedding videos or tracking playback. */
export function RecommendedStudyChannels() {
    return (
        <section
            className="study-channels"
            aria-label="Recommended Study Channels"
        >
            <span className="eyebrow">KEEP EXPLORING</span>
            <h2>Recommended Study Channels</h2>
            <p>
                Fresh perspectives for your TOEFL preparation. Explore these
                independent resources at your own pace.
            </p>
            <ul>
                {channels.map((channel) => (
                    <li key={channel}>
                        <a
                            href={`https://www.youtube.com/@${channel}`}
                            target="_blank"
                            rel="noopener noreferrer"
                        >
                            <span className="channel-play" aria-hidden="true">
                                ▶
                            </span>
                            <span>@{channel}</span>
                            <span aria-hidden="true">↗</span>
                            <span className="sr-only">
                                {" "}
                                (opens in a new tab)
                            </span>
                        </a>
                    </li>
                ))}
            </ul>
        </section>
    );
}

/** Displays consistent attribution and contact access on both public and authenticated pages. */
export function Footer({ onContact }: { onContact: () => void }) {
    return (
        <footer className="app-footer no-print">
            <a
                href="https://www.linkedin.com/in/berşan-gülce-yurtseven-48856322b"
                target="_blank"
                rel="noopener noreferrer"
            >
                Made by Berşan Gülce Yurtseven
            </a>
            <button className="text-button" onClick={onContact}>
                Contact
            </button>
        </footer>
    );
}

/** Sends bounded plain-text feedback; the native dialog traps focus and restores it on close. */
export function ContactDialog({
    feedback,
    onClose,
}: {
    feedback: boolean;
    onClose: () => void;
}) {
    const ref = useRef<HTMLDialogElement>(null);
    const [subject, setSubject] = useState("");
    const [message, setMessage] = useState("");
    const [email, setEmail] = useState(getSession()?.email ?? "");
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const [sent, setSent] = useState(false);
    useEffect(() => {
        const dialog = ref.current!;
        dialog.showModal();
        return () => {
            dialog.close();
            // The feedback action lives inside a closed disclosure, so restore its visible summary explicitly.
            if (feedback)
                document
                    .querySelector<HTMLElement>(".profile-menu summary")
                    ?.focus();
        };
    }, [feedback]);
    /** Keeps drafts visible on failed delivery and reports success only after the server accepts the email. */
    async function submit(event: FormEvent) {
        event.preventDefault();
        setError("");
        setBusy(true);
        try {
            await csrfPost("/api/contact", { subject, message, email });
            setSent(true);
        } catch (e) {
            setError((e as Error).message);
        } finally {
            setBusy(false);
        }
    }
    return (
        <dialog
            ref={ref}
            className="modal contact-modal"
            aria-labelledby="contact-title"
            onCancel={(e) => {
                e.preventDefault();
                if (!busy) onClose();
            }}
        >
            <div className="contact-heading">
                <h2 id="contact-title">
                    {feedback ? "Feedback & Suggestions" : "Contact"}
                </h2>
                <button
                    className="icon-button"
                    aria-label="Close contact form"
                    onClick={onClose}
                    disabled={busy}
                >
                    ×
                </button>
            </div>
            {sent ? (
                <>
                    <p role="status">
                        Your message has been sent. Thank you for helping us
                        improve.
                    </p>
                    <button className="primary" onClick={onClose}>
                        Done
                    </button>
                </>
            ) : (
                <form onSubmit={submit}>
                    <p>
                        Share a question or an idea with the TOEFL Studio team.
                    </p>
                    <label htmlFor="contact-email">Reply email</label>
                    <input
                        id="contact-email"
                        autoFocus
                        required
                        type="email"
                        autoComplete="email"
                        maxLength={256}
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        disabled={busy}
                    />
                    <label htmlFor="contact-subject">Subject</label>
                    <input
                        id="contact-subject"
                        required
                        minLength={3}
                        maxLength={120}
                        value={subject}
                        onChange={(e) => setSubject(e.target.value)}
                        disabled={busy}
                    />
                    <label htmlFor="contact-message">Message</label>
                    <textarea
                        id="contact-message"
                        required
                        minLength={10}
                        maxLength={5000}
                        rows={6}
                        value={message}
                        onChange={(e) => setMessage(e.target.value)}
                        disabled={busy}
                    />
                    <p className="field-hint">
                        Your message and reply email will be sent to the
                        platform administrator.
                    </p>
                    {error && <ErrorBox message={error} />}
                    <button className="primary full" disabled={busy}>
                        {busy ? "Sending…" : "Send message"}
                    </button>
                </form>
            )}
        </dialog>
    );
}
