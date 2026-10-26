import { useEffect, useRef } from "react";
import type { Session } from "./api";
import { getSession, request, setSession, signOut } from "./api";
import { Icon, navigate } from "./components";

/** Shows the saved display name and an accessible profile disclosure with account actions. */
export function AccountMenu({
    session,
    onFeedback,
}: {
    session: Session;
    onFeedback: () => void;
}) {
    const ref = useRef<HTMLDetailsElement>(null);
    useEffect(() => {
        if (session.displayName !== undefined) return;
        const controller = new AbortController();
        request<{ email: string; username: string; displayName: string }>(
            "/api/auth/manage/info",
            { signal: controller.signal },
        )
            .then((profile) => {
                if (getSession()?.accessToken === session.accessToken)
                    setSession({ ...session, ...profile });
            })
            .catch(() => {
                /* The menu remains usable if profile loading is temporarily unavailable. */
            });
        return () => controller.abort();
    }, [session]);
    useEffect(() => {
        /** Closes the disclosure for outside clicks, Escape and route changes without trapping normal tab navigation. */
        const close = (event: Event) => {
            const menu = ref.current;
            if (!menu) return;
            if (event instanceof KeyboardEvent && event.key !== "Escape")
                return;
            if (
                event.type === "pointerdown" &&
                menu.contains(event.target as Node)
            )
                return;
            if (event.type === "keydown" && menu.open)
                menu.querySelector("summary")?.focus();
            menu.open = false;
        };
        document.addEventListener("pointerdown", close);
        document.addEventListener("keydown", close);
        window.addEventListener("hashchange", close);
        return () => {
            document.removeEventListener("pointerdown", close);
            document.removeEventListener("keydown", close);
            window.removeEventListener("hashchange", close);
        };
    }, []);
    const name =
        session.displayName?.trim() ||
        (session.username?.includes("@") ? "Learner" : session.username) ||
        "Learner";
    return (
        <details className="profile-menu" ref={ref}>
            <summary aria-label="Account menu">
                <span className="avatar">
                    {name[0].toLocaleUpperCase("en")}
                </span>
                <span className="account-email">{name}</span>
                <span aria-hidden="true">⌄</span>
            </summary>
            <div className="profile-dropdown">
                <a
                    href="#/settings"
                    onClick={() => {
                        ref.current!.open = false;
                    }}
                >
                    <Icon name="settings" size={18} />
                    Settings
                </a>
                <button
                    onClick={() => {
                        ref.current!.open = false;
                        // Restore a visible trigger before the modal captures its return-focus target.
                        ref.current?.querySelector("summary")?.focus();
                        onFeedback();
                    }}
                >
                    <Icon name="chat" size={18} />
                    Feedback &amp; Suggestions
                </button>
                <hr />
                <button
                    onClick={() => {
                        signOut();
                        navigate("/");
                    }}
                >
                    <Icon name="exit" size={18} />
                    Sign out
                </button>
            </div>
        </details>
    );
}
