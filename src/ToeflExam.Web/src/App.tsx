import { ContactDialog, Footer, Notebook } from "./Community";
import { AccountMenu } from "./AccountMenu";
import { StudioDashboard } from "./StudioDashboard";
import { Settings } from "./Settings";
import { VocabularyStudio } from "./Vocabulary";
import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { ApiError, getSession, post, request, finishSignIn } from "./api";
import { ErrorBox, Icon, navigate } from "./components";
import { Dashboard } from "./Dashboard";
import { Practice } from "./Practice";
import { isModule } from "./types";
import { Studios } from "./Studios";
import { Listening } from "./Listening";
import { BankDashboard, BankPractice } from "./BankPractice";
import { SpeakingPractice, isSpeakingModule } from "./Speaking";
import { ReadingPractice, isReadingModule } from "./Reading";

/** Routes authenticated practice pages and exposes shared account, contact and footer controls. */
export function App() {
    const [session, updateSession] = useState(getSession);
    const [route, setRoute] = useState(() => location.hash.slice(1) || "/");
    useEffect(() => {
        /** Updates the rendered account when session state changes. */
        const changed = () => updateSession(getSession());
        /** Reads the current hash route for the application shell. */
        const routing = () => setRoute(location.hash.slice(1) || "/");
        window.addEventListener("session-change", changed);
        window.addEventListener("hashchange", routing);
        return () => {
            window.removeEventListener("session-change", changed);
            window.removeEventListener("hashchange", routing);
        };
    }, []);
    useEffect(() => {
        document.getElementById("page-title")?.focus();
    }, [route]);
    const [contact, setContact] = useState<"contact" | "feedback" | null>(null);
    const dialog = contact && (
        <ContactDialog
            feedback={contact === "feedback"}
            onClose={() => setContact(null)}
        />
    );
    if (!session)
        return (
            <>
                <Auth onContact={() => setContact("contact")} />
                {dialog}
            </>
        );
    const match = route.match(/^\/practice\/([^/]+)\/([a-f\d-]{36})\/(\d+)$/i);
    const practicing = !!match && isModule(match[1]);
    const readingMatch = route.match(
        /^\/reading\/([^/]+)\/([a-f\d-]{36})\/(\d+)$/i,
    );
    const readingPractice = !!readingMatch && isReadingModule(readingMatch[1]);
    const speakingMatch = route.match(
        /^\/speaking\/([^/]+)\/([a-f\d-]{36})\/(\d+)$/i,
    );
    const speakingPractice =
        !!speakingMatch && isSpeakingModule(speakingMatch[1]);
    const bankMatch = route.match(/^\/session\/([a-f\d-]{36})$/i);
    return (
        <div className="app-shell">
            <a
                className="skip-link"
                href="#main"
                onClick={(event) => {
                    event.preventDefault();
                    document.getElementById("main")?.focus();
                }}
            >
                Skip to content{" "}
            </a>
            <aside className="sidebar no-print">
                <a className="brand" href="#/" aria-label="TOEFL Studio home">
                    <span className="brand-icon">
                        <Icon name="pen" size={23} />
                    </span>
                    <span>
                        toefl<span className="brand-studio">studio</span>
                    </span>
                </a>
                <div className="sidebar-label">YOUR WORKSPACE</div>
                <nav aria-label="Main navigation">
                    <a
                        className={
                            "nav-link " + (route === "/exams" ? "active" : "")
                        }
                        href="#/exams"
                    >
                        <Icon name="history" />
                        Practice Exams
                    </a>
                    <a
                        className={
                            route === "/" ? "nav-link active" : "nav-link"
                        }
                        href="#/"
                    >
                        <Icon name="grid" />
                        Studios{" "}
                    </a>
                    <a
                        className={
                            "nav-link " +
                            (route.startsWith("/reading") ? "active" : "")
                        }
                        href="#/reading"
                    >
                        <Icon name="book" />
                        Reading Studio
                    </a>
                    <a
                        className={
                            "nav-link " +
                            (route === "/listening" ? "active" : "")
                        }
                        href="#/listening"
                    >
                        <Icon name="headphones" />
                        Listening Studio
                    </a>
                    <a
                        className={
                            "nav-link " +
                            (route === "/writing" || practicing ? "active" : "")
                        }
                        href="#/writing"
                    >
                        <Icon name="pen" />
                        Writing Studio
                    </a>
                    <a
                        className={
                            "nav-link " +
                            (route.startsWith("/speaking") ? "active" : "")
                        }
                        href="#/speaking"
                    >
                        <Icon name="mic" />
                        Speaking Studio
                    </a>
                    <a
                        className={
                            "nav-link " +
                            (route === "/vocabulary" ? "active" : "")
                        }
                        href="#/vocabulary"
                    >
                        <Icon name="book" />
                        Vocabulary Studio
                    </a>
                </nav>

                <div className="sidebar-footer">
                    <span className="tiny-dot" /> TOEFL preparation{" "}
                    <small>Independent practice platform</small>
                </div>
            </aside>
            <div className="app-body">
                <header className="topbar no-print">
                    <div className="breadcrumb">
                        Workspace <span>/</span>{" "}
                        <strong>
                            {route === "/settings"
                                ? "Settings"
                                : route === "/vocabulary"
                                  ? "Vocabulary"
                                  : route === "/exams" || bankMatch
                                    ? "Practice"
                                    : route.startsWith("/reading")
                                      ? "Reading"
                                      : route.startsWith("/speaking")
                                        ? "Speaking"
                                        : route === "/listening"
                                          ? "Listening"
                                          : route === "/"
                                            ? "Studios"
                                            : "Writing"}
                        </strong>
                    </div>
                    <AccountMenu
                        session={session}
                        onFeedback={() => setContact("feedback")}
                    />
                </header>
                <main
                    id="main"
                    lang="en"
                    tabIndex={-1}
                    className={
                        practicing || readingPractice || speakingPractice
                            ? "main practice-main"
                            : "main"
                    }
                >
                    {route === "/settings" ? (
                        <Settings />
                    ) : route === "/vocabulary" ? (
                        <VocabularyStudio />
                    ) : route === "/" ? (
                        <Studios />
                    ) : route === "/exams" ? (
                        <BankDashboard mode="exam" />
                    ) : bankMatch ? (
                        <BankPractice
                            key={session.email + bankMatch[1]}
                            id={bankMatch[1]}
                        />
                    ) : route === "/reading" ? (
                        <StudioDashboard section="reading" />
                    ) : route === "/reading/history" ? (
                        <StudioDashboard section="reading" />
                    ) : readingPractice &&
                      readingMatch &&
                      isReadingModule(readingMatch[1]) ? (
                        <ReadingPractice
                            key={session.email + route}
                            module={readingMatch[1]}
                            runId={readingMatch[2]}
                            position={Number(readingMatch[3])}
                            email={session.email}
                        />
                    ) : route === "/listening" ? (
                        <Listening />
                    ) : route === "/speaking" ? (
                        <StudioDashboard section="speaking" />
                    ) : route === "/speaking/history" ? (
                        <StudioDashboard section="speaking" />
                    ) : speakingPractice &&
                      speakingMatch &&
                      isSpeakingModule(speakingMatch[1]) ? (
                        <SpeakingPractice
                            key={session.email + route}
                            module={speakingMatch[1]}
                            runId={speakingMatch[2]}
                            position={Number(speakingMatch[3])}
                        />
                    ) : route === "/writing" ? (
                        <Dashboard />
                    ) : route === "/history" ? (
                        <Dashboard />
                    ) : practicing && match && isModule(match[1]) ? (
                        <Practice
                            key={session.email + route}
                            module={match[1]}
                            runId={match[2]}
                            position={Number(match[3])}
                            email={session.email}
                        />
                    ) : (
                        <div className="empty-state">
                            <h1 id="page-title" tabIndex={-1}>
                                Page not found.{" "}
                            </h1>
                            <button
                                className="primary"
                                onClick={() => navigate("/")}
                            >
                                Back to studio{" "}
                            </button>
                        </div>
                    )}
                </main>
                <Footer onContact={() => setContact("contact")} />
                {dialog}
            </div>
        </div>
    );
}
/** Presents password registration and sign-in using the existing account credentials. */
function Auth({ onContact }: { onContact: () => void }) {
    const [register, setRegister] = useState(false);
    const [email, setEmail] = useState("");
    const [username, setUsername] = useState("");
    const [displayName, setDisplayName] = useState("");
    const [password, setPassword] = useState("");
    const [confirmation, setConfirmation] = useState("");
    const [error, setError] = useState("");
    const [notice, setNotice] = useState("");
    const [busy, setBusy] = useState(false);
    const [showPassword, setShowPassword] = useState(false);
    /** Validates confirmation locally, then delegates credentials and identity validation to the API. */
    async function submit(event: FormEvent) {
        event.preventDefault();
        setError("");
        setNotice("");
        if (register && password !== confirmation) {
            setError("Passwords do not match. Please check them again.");
            return;
        }
        setBusy(true);
        try {
            const credentials = { email: email.trim(), password };
            if (register) {
                await post(
                    "/api/auth/register",
                    {
                        ...credentials,
                        username: username.trim(),
                        displayName: displayName.trim() || username.trim(),
                    },
                    false,
                );
                setRegister(false);
                setPassword("");
                setConfirmation("");
                setNotice(
                    "Your account is ready. Enter your password to sign in.",
                );
            } else {
                const tokens = await post<{
                    accessToken: string;
                    refreshToken: string;
                }>("/api/auth/login?useCookies=false", credentials, false);
                await finishSignIn(tokens);
                navigate("/");
            }
        } catch (e) {
            setError(
                e instanceof ApiError
                    ? ({
                          0: "Unable to reach the server. Check your connection.",
                          401: "Incorrect email, username or password.",
                          429: "Too many attempts. Please wait a minute and try again.",
                          400: "Unable to create this account. Check your username, email and password requirements, or sign in if you already have an account.",
                      }[e.status] ??
                          "Unable to complete your request. Please try again.")
                    : "Unable to sign in. Please try again.",
            );
        } finally {
            setBusy(false);
        }
    }
    return (
        <div className="auth-layout" lang="en">
            <section className="auth-story">
                <div className="brand">
                    <span className="brand-icon">
                        <Icon name="pen" size={23} />
                    </span>
                    <span>
                        toefl<span className="brand-studio">studio</span>
                    </span>
                </div>
                <div className="auth-story-content">
                    <span className="eyebrow">PREPARE FOR TOEFL iBT</span>
                    <h1>
                        Build your skills. <br />
                        Prepare with <br />
                        <em>confidence.</em>
                    </h1>
                    <p>
                        Explore TOEFL task types. Practice with purpose.
                        <br />
                        Review feedback and track your progress.
                    </p>
                    <Notebook />
                </div>
                <div className="auth-bottom">
                    Four skills. One focused TOEFL preparation space.
                </div>
            </section>
            <section className="auth-form-panel">
                <div className="auth-form-wrap">
                    <span className="eyebrow">WELCOME TO TOEFL STUDIO</span>
                    <h2>
                        {register
                            ? "Create your account."
                            : "Continue your preparation."}
                    </h2>
                    <p className="muted">
                        {register
                            ? "Create an account to explore TOEFL practice and track your progress."
                            : "Sign in to review your progress and continue your TOEFL practice."}
                    </p>
                    <div className="auth-tabs">
                        <button
                            className={!register ? "selected" : ""}
                            disabled={busy}
                            onClick={() => {
                                setRegister(false);
                                setError("");
                            }}
                        >
                            Sign in
                        </button>
                        <button
                            className={register ? "selected" : ""}
                            disabled={busy}
                            onClick={() => {
                                setRegister(true);
                                setError("");
                            }}
                        >
                            Create account
                        </button>
                    </div>
                    <form onSubmit={submit}>
                        {register && (
                            <>
                                <label htmlFor="username">Username</label>
                                <input
                                    id="username"
                                    required
                                    minLength={3}
                                    maxLength={30}
                                    pattern="[A-Za-z][A-Za-z0-9_.\-]{2,29}"
                                    autoComplete="username"
                                    value={username}
                                    onChange={(e) =>
                                        setUsername(e.target.value)
                                    }
                                    disabled={busy}
                                    aria-describedby="username-hint"
                                />
                                <p className="field-hint" id="username-hint">
                                    3–30 characters. Start with a letter; use
                                    letters, numbers, dots, underscores or
                                    hyphens.
                                </p>
                                <label htmlFor="signup-display-name">
                                    Display name
                                </label>
                                <input
                                    id="signup-display-name"
                                    required
                                    maxLength={100}
                                    autoComplete="nickname"
                                    value={displayName}
                                    onChange={(e) =>
                                        setDisplayName(e.target.value)
                                    }
                                    disabled={busy}
                                />
                            </>
                        )}
                        <label htmlFor="email">
                            {register ? "Email address" : "Email or username"}
                        </label>
                        <input
                            id="email"
                            type={register ? "email" : "text"}
                            maxLength={256}
                            required
                            autoComplete={register ? "email" : "username"}
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            placeholder="you@example.com"
                            disabled={busy}
                        />
                        <div className="label-row">
                            <label htmlFor="password">Password</label>
                            <button
                                className="text-button"
                                type="button"
                                onClick={() => setShowPassword(!showPassword)}
                            >
                                {showPassword ? "Hide" : "Show"}
                            </button>
                        </div>
                        <input
                            id="password"
                            type={showPassword ? "text" : "password"}
                            required
                            minLength={register ? 10 : undefined}
                            autoComplete={
                                register ? "new-password" : "current-password"
                            }
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            disabled={busy}
                            aria-describedby={
                                register ? "password-hint" : undefined
                            }
                        />
                        {register && (
                            <>
                                <p className="field-hint" id="password-hint">
                                    At least 10 characters, including uppercase
                                    and lowercase letters, a number, and a
                                    symbol.
                                </p>
                                <label htmlFor="confirmation">
                                    Confirm password
                                </label>
                                <input
                                    id="confirmation"
                                    type="password"
                                    required
                                    autoComplete="new-password"
                                    value={confirmation}
                                    onChange={(e) =>
                                        setConfirmation(e.target.value)
                                    }
                                    disabled={busy}
                                />
                            </>
                        )}
                        {error && <ErrorBox message={error} />}
                        {notice && (
                            <div className="info-banner" role="status">
                                {notice}
                            </div>
                        )}
                        <button className="primary full" disabled={busy}>
                            {busy
                                ? "Please wait…"
                                : register
                                  ? "Create my account"
                                  : "Sign in to TOEFL Studio"}
                            {!busy && <Icon name="arrow" size={18} />}
                        </button>
                    </form>

                    <div className="auth-fine">
                        <Icon name="lock" size={15} />
                        <span>
                            Your progress belongs to your account. Your session
                            is stored in this browser tab.
                        </span>
                    </div>
                </div>
                <Footer onContact={onContact} />
            </section>
        </div>
    );
}
