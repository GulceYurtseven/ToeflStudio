import { useEffect, useState } from "react";
import { request, post, signOut, getSession, setSession } from "./api";
import { ErrorBox, Spinner, navigate } from "./components";
import "./studio.css";
type Profile = {
    email: string;
    username?: string;
    hasPassword?: boolean;
    displayName: string;
    dailyPracticeMinutes: number;
    targetTestDate: string | null;
    membership: string;
    membershipDescription: string;
};
/** Edits the current account profile, study goals and supported credential settings. */
export function Settings() {
    const [profile, setProfile] = useState<Profile | null>(null),
        [error, setError] = useState(""),
        [notice, setNotice] = useState(""),
        [busy, setBusy] = useState(false),
        [currentPassword, setCurrentPassword] = useState(""),
        [newEmail, setNewEmail] = useState(""),
        [newPassword, setNewPassword] = useState(""),
        [confirm, setConfirm] = useState("");
    useEffect(() => {
        const c = new AbortController();
        request<Profile>("/api/settings", { signal: c.signal })
            .then(setProfile)
            .catch((e) => {
                if (!c.signal.aborted) setError(e.message);
            });
        return () => c.abort();
    }, []);
    /** Saves profile changes or updates credentials and requires a fresh sign-in. */
    async function save(credentials: boolean) {
        setError("");
        setNotice("");
        if (credentials && newPassword !== confirm) {
            setError("The new passwords do not match.");
            return;
        }
        setBusy(true);
        try {
            if (credentials) {
                await post("/api/settings/credentials", {
                    currentPassword,
                    newEmail: newEmail.trim() || null,
                    newPassword: newPassword || null,
                });
                signOut();
                navigate("/");
            } else {
                await post("/api/settings/profile", profile);
                const session = getSession();
                if (session && profile)
                    setSession({
                        ...session,
                        displayName: profile.displayName.trim(),
                    });
                setNotice("Your profile and study goals have been saved.");
            }
        } catch (e) {
            setError((e as Error).message);
        } finally {
            setBusy(false);
        }
    }
    return (
        <section>
            <div className="page-heading">
                <div>
                    <span className="eyebrow">YOUR ACCOUNT</span>
                    <h1 id="page-title" tabIndex={-1}>
                        Settings
                    </h1>
                    <p>
                        Manage your profile, study goals, and sign-in details.
                    </p>
                </div>
            </div>
            {error && <ErrorBox message={error} />}{" "}
            {notice && (
                <p className="info-banner" role="status">
                    {notice}
                </p>
            )}
            {!profile && !error && <Spinner />}
            {profile && (
                <div className="settings-grid">
                    <form
                        className="settings-card"
                        onSubmit={(e) => {
                            e.preventDefault();
                            void save(false);
                        }}
                    >
                        <h2>Profile and study goals</h2>
                        <label htmlFor="display-name">Display name</label>
                        <input
                            id="display-name"
                            required
                            maxLength={100}
                            value={profile.displayName}
                            onChange={(e) =>
                                setProfile({
                                    ...profile,
                                    displayName: e.target.value,
                                })
                            }
                            disabled={busy}
                        />
                        <label htmlFor="daily-goal">
                            Daily practice goal (minutes)
                        </label>
                        <input
                            id="daily-goal"
                            type="number"
                            min={5}
                            max={180}
                            required
                            value={profile.dailyPracticeMinutes}
                            onChange={(e) =>
                                setProfile({
                                    ...profile,
                                    dailyPracticeMinutes: Number(
                                        e.target.value,
                                    ),
                                })
                            }
                            disabled={busy}
                        />
                        <label htmlFor="test-date">
                            Target test date (optional)
                        </label>
                        <input
                            id="test-date"
                            type="date"
                            value={profile.targetTestDate ?? ""}
                            onChange={(e) =>
                                setProfile({
                                    ...profile,
                                    targetTestDate: e.target.value || null,
                                })
                            }
                            disabled={busy}
                        />
                        <button className="primary" disabled={busy}>
                            Save profile
                        </button>
                    </form>
                    <section className="settings-card">
                        <h2>Membership</h2>
                        <strong>{profile.membership} membership</strong>
                        <p>{profile.membershipDescription}</p>
                        <p>
                            There are no paid plans or billing subscriptions to
                            manage.
                        </p>
                        <p>
                            Signed in as <strong>{profile.email}</strong>
                        </p>
                    </section>
                    <form
                        className="settings-card"
                        onSubmit={(e) => {
                            e.preventDefault();
                            void save(true);
                        }}
                    >
                        <h2>Email and password</h2>
                        {profile.hasPassword === false && (
                            <p className="info-banner">
                                You sign in with Google. Manage your password
                                and email through your Google account.
                            </p>
                        )}
                        <fieldset
                            disabled={profile.hasPassword === false}
                            style={{
                                border: 0,
                                padding: 0,
                                margin: 0,
                                minWidth: 0,
                            }}
                        >
                            <p>
                                Confirm your current password to change your
                                sign-in details. Sign in again after saving.
                            </p>
                            <label htmlFor="current-password">
                                Current password
                            </label>
                            <input
                                id="current-password"
                                type="password"
                                autoComplete="current-password"
                                required
                                maxLength={256}
                                value={currentPassword}
                                onChange={(e) =>
                                    setCurrentPassword(e.target.value)
                                }
                                disabled={busy}
                            />
                            <label htmlFor="new-email">
                                New email address (optional)
                            </label>
                            <input
                                id="new-email"
                                type="email"
                                autoComplete="email"
                                value={newEmail}
                                onChange={(e) => setNewEmail(e.target.value)}
                                disabled={busy}
                            />
                            <label htmlFor="new-password">
                                New password (optional)
                            </label>
                            <input
                                id="new-password"
                                type="password"
                                autoComplete="new-password"
                                minLength={10}
                                maxLength={256}
                                value={newPassword}
                                onChange={(e) => setNewPassword(e.target.value)}
                                disabled={busy}
                            />
                            <p className="field-hint">
                                Use at least 10 characters, with uppercase and
                                lowercase letters, a number, and a symbol.
                            </p>
                            <label htmlFor="confirm-password">
                                Confirm new password
                            </label>
                            <input
                                id="confirm-password"
                                type="password"
                                autoComplete="new-password"
                                value={confirm}
                                onChange={(e) => setConfirm(e.target.value)}
                                disabled={busy}
                            />
                            <button
                                className="primary"
                                disabled={
                                    busy || (!newEmail.trim() && !newPassword)
                                }
                            >
                                Update sign-in details
                            </button>
                        </fieldset>
                    </form>
                </div>
            )}
        </section>
    );
}
