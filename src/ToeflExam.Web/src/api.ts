export interface Session {
    accessToken: string;
    refreshToken: string;
    email: string;
    displayName?: string;
    username?: string;
}
const key = "toefl.session";
let session: Session | null = readSession();
let refreshing: Promise<boolean> | null = null;
/** Restores a valid tab-scoped bearer session without trusting malformed storage. */
export function readSession(): Session | null {
    try {
        const data = JSON.parse(sessionStorage.getItem(key) || "null");
        return data &&
            typeof data.accessToken === "string" &&
            typeof data.refreshToken === "string" &&
            typeof data.email === "string"
            ? data
            : null;
    } catch {
        return null;
    }
}
/** Returns the current in-memory account session. */
export function getSession() {
    return session;
}
/** Synchronizes the current session with tab storage and notifies mounted account views. */
export function setSession(value: Session | null) {
    session = value;
    try {
        value
            ? sessionStorage.setItem(key, JSON.stringify(value))
            : sessionStorage.removeItem(key);
    } catch {
        /* In-memory session still works. */
    }
    window.dispatchEvent(new Event("session-change"));
}
/** Clears the active session and private practice drafts from this browser tab. */
export function signOut() {
    setSession(null);
    for (const k of Object.keys(sessionStorage))
        if (k.startsWith("toefl.draft.")) sessionStorage.removeItem(k);
}
export class ApiError extends Error {
    /** Creates an API failure with an HTTP status and a safe user-facing message. */
    constructor(
        public status: number,
        message: string,
    ) {
        super(message);
    }
}
/** Coalesces concurrent refresh requests and never restores a session after logout. */
async function refresh() {
    if (refreshing) return refreshing;
    const current = session;
    refreshing = (async () => {
        if (!current) return false;
        try {
            const response = await fetch("/api/auth/refresh", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ refreshToken: current.refreshToken }),
            });
            if (!response.ok) {
                if (session === current && response.status === 401)
                    setSession(null);
                return false;
            }
            const tokens = await response.json();
            if (session !== current) return false; // Never restore a session after logout.
            setSession({ ...current, ...tokens });
            return true;
        } catch {
            return false;
        }
    })().finally(() => {
        refreshing = null;
    });
    return refreshing;
}
/** Sends an API request, refreshes expired bearer credentials once, and normalizes safe errors. */
export async function request<T>(
    url: string,
    options: RequestInit = {},
    authenticated = true,
    responseType: "json" | "blob" = "json",
): Promise<T> {
    /** Sends the request with the current bearer token and caller-supplied headers. */
    const send = () =>
        fetch(url, {
            ...options,
            headers: {
                ...(options.body ? { "Content-Type": "application/json" } : {}),
                ...(authenticated && session
                    ? { Authorization: "Bearer " + session.accessToken }
                    : {}),
                ...options.headers,
            },
        });
    let response: Response;
    try {
        response = await send();
        if (
            response.status === 401 &&
            authenticated &&
            session &&
            (await refresh())
        )
            response = await send();
    } catch (error) {
        if (error instanceof DOMException && error.name === "AbortError")
            throw error;
        throw new ApiError(
            0,
            "Unable to reach the server. Check your connection; your response remains in this tab.",
        );
    }
    if (!response.ok) {
        if (response.status === 401 && authenticated) setSession(null);
        let data: {
            detail?: string;
            title?: string;
            errors?: Record<string, string[]>;
        } = {};
        try {
            data = await response.json();
        } catch {
            /* Plain-text errors use the status fallback. */
        }
        const messages: Record<number, string> = {
            401: authenticated
                ? "Your session could not be refreshed. Please sign in again."
                : "Incorrect email or password.",
            404: "This activity is unavailable or does not belong to your account.",
            409: "This action is currently unavailable. Refresh the current status.",
            410: "Detailed feedback has expired. Your score and progress are saved.",
            429: "Too many requests. Wait a minute and try again.",
            502: "Assessment could not be completed. Your response is preserved; please retry.",
            503: "Assessment is temporarily unavailable. Your response is preserved.",
        };
        const validation =
            data.errors && Object.values(data.errors).flat().join(" ");
        throw new ApiError(
            response.status,
            ((response.status === 502 || response.status === 503) &&
                data.detail) ||
                (response.status === 409 && data.detail) ||
                messages[response.status] ||
                validation ||
                data.detail ||
                data.title ||
                "The operation could not be completed. Please retry.",
        );
    }
    if (
        response.status === 204 ||
        response.headers.get("content-length") === "0"
    )
        return undefined as T;
    if (responseType === "blob") return (await response.blob()) as T;
    const text = await response.text();
    return text ? (JSON.parse(text) as T) : (undefined as T);
}
/** Serializes an optional JSON payload through the shared API client. */
export const post = <T>(url: string, body?: unknown, authenticated = true) =>
    request<T>(
        url,
        {
            method: "POST",
            ...(body !== undefined ? { body: JSON.stringify(body) } : {}),
        },
        authenticated,
    );

export interface Draft {
    answer: string;
    tiles: number[];
    submissionId: string;
    attempted: boolean;
}
/** Scopes a temporary draft to an account, run and activity position. */
export function draftKey(email: string, runId: string, position: number) {
    return "toefl.draft." + email + "." + runId + "." + position;
}
/** Restores an editable draft or creates a new idempotent submission identifier. */
export function loadDraft(key: string): Draft {
    try {
        const value = JSON.parse(sessionStorage.getItem(key) || "null");
        if (
            value &&
            typeof value.answer === "string" &&
            Array.isArray(value.tiles) &&
            value.tiles.every((id: unknown) => Number.isInteger(id)) &&
            typeof value.submissionId === "string"
        )
            return { ...value, attempted: value.attempted === true };
    } catch {
        /* Start an empty draft if browser storage is unavailable. */
    }
    return {
        answer: "",
        tiles: [],
        submissionId: crypto.randomUUID(),
        attempted: false,
    };
}
/** Persists a draft in this tab and reports browser storage failures. */
export function saveDraft(key: string, draft: Draft): boolean {
    try {
        sessionStorage.setItem(key, JSON.stringify(draft));
        return true;
    } catch {
        return false;
    }
}

/** Loads the server's canonical account identity before storing a newly issued bearer session. */
export async function finishSignIn(tokens: {
    accessToken: string;
    refreshToken: string;
}) {
    const profile = await request<{
        email: string;
        username: string;
        displayName: string;
    }>(
        "/api/auth/manage/info",
        { headers: { Authorization: "Bearer " + tokens.accessToken } },
        false,
    );
    setSession({ ...tokens, ...profile });
}

/** Fetches a same-origin antiforgery token for forms that use cookies or allow anonymous submission. */
export async function csrfPost<T>(url: string, body: unknown): Promise<T> {
    const csrf = await request<{ token: string }>("/api/auth/csrf", {}, false);
    return request<T>(
        url,
        {
            method: "POST",
            headers: { "X-CSRF-TOKEN": csrf.token },
            body: JSON.stringify(body),
        },
        false,
    );
}
