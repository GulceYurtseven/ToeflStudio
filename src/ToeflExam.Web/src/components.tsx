import { useEffect, useRef } from "react";
import type { ReactNode } from "react";
/** Renders an icon from the shared vector set with consistent sizing. */
export function Icon({ name, size = 20 }: { name: string; size?: number }) {
    const paths: Record<string, ReactNode> = {
        settings: (
            <>
                <path d="M4 6h16M4 12h16M4 18h16" />
                <circle cx="9" cy="6" r="2" />
                <circle cx="15" cy="12" r="2" />
                <circle cx="9" cy="18" r="2" />
            </>
        ),
        mic: (
            <>
                <rect x="8" y="2" width="8" height="13" rx="4" />
                <path d="M5 10v2a7 7 0 0 0 14 0v-2M12 19v3m-4 0h8" />
            </>
        ),
        headphones: (
            <>
                <path d="M3 14v-3a9 9 0 0 1 18 0v3" />
                <rect x="2" y="12" width="5" height="9" rx="2" />
                <rect x="17" y="12" width="5" height="9" rx="2" />
            </>
        ),
        pen: (
            <>
                <path d="m15 4 5 5M4 20l4-1L20 7a2 2 0 0 0-5-5L3 14l-1 7Z" />
                <path d="M13 20h8" />
            </>
        ),
        grid: (
            <>
                <rect x="3" y="3" width="7" height="7" rx="1.5" />
                <rect x="14" y="3" width="7" height="7" rx="1.5" />
                <rect x="3" y="14" width="7" height="7" rx="1.5" />
                <rect x="14" y="14" width="7" height="7" rx="1.5" />
            </>
        ),
        tiles: (
            <>
                <rect x="2" y="5" width="9" height="6" rx="2" />
                <rect x="14" y="5" width="8" height="6" rx="2" />
                <rect x="5" y="14" width="14" height="6" rx="2" />
            </>
        ),
        mail: (
            <>
                <rect x="3" y="5" width="18" height="14" rx="3" />
                <path d="m3 7 9 6 9-6" />
            </>
        ),
        chat: <path d="M21 11a8 8 0 0 1-8 8H7l-5 3 2-6a8 8 0 1 1 17-5Z" />,
        arrow: <path d="M4 12h16m-6-6 6 6-6 6" />,
        back: <path d="M20 12H4m6-6-6 6 6 6" />,
        check: <path d="m5 12 4 4L19 6" />,
        lock: (
            <>
                <rect x="5" y="10" width="14" height="11" rx="2" />
                <path d="M8 10V6a4 4 0 0 1 8 0v4" />
            </>
        ),
        history: (
            <>
                <path d="M3 4v5h5M3 9a9 9 0 1 1 0 7" />
                <path d="M12 7v5l3 2" />
            </>
        ),
        exit: (
            <>
                <path d="M9 3H4v18h5M10 12h11m-4-4 4 4-4 4" />
            </>
        ),
        download: (
            <>
                <path d="M12 3v12m-4-4 4 4 4-4M4 16v5h16v-5" />
            </>
        ),
        refresh: (
            <>
                <path d="M21 3v6h-6M3 21v-6h6M4 8a9 9 0 0 1 15-4l2 5M3 15l2 5a9 9 0 0 0 15-4" />
            </>
        ),
        spark: (
            <>
                <path d="m12 3 2.5 6.5L21 12l-6.5 2.5L12 21l-2.5-6.5L3 12l6.5-2.5Z" />
            </>
        ),
        book: (
            <>
                <path d="M12 5v16M12 5C8 2 5 3 2 3v16c4 0 7 0 10 2 3-2 6-2 10-2V3c-3 0-6-1-10 2Z" />
            </>
        ),
    };
    return (
        <svg
            width={size}
            height={size}
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.7"
            strokeLinecap="round"
            strokeLinejoin="round"
            aria-hidden="true"
        >
            {paths[name] || paths.pen}
        </svg>
    );
}
/** Announces loading status with a compact visual indicator. */
export function Spinner({ text = "Loading…" }: { text?: string }) {
    return (
        <div className="loading" role="status">
            <span className="spinner" />
            {text}
        </div>
    );
}
/** Displays a recoverable error and an optional retry action. */
export function ErrorBox({
    message,
    retry,
    retryLabel = "Try again",
}: {
    message: string;
    retry?: () => void;
    retryLabel?: string;
}) {
    return (
        <div className="error-box" role="alert">
            <span>{message}</span>
            {retry && (
                <button className="text-button" onClick={retry}>
                    {retryLabel}
                </button>
            )}
        </div>
    );
}
/** Renders an accessible completion bar with a named percentage value. */
export function Progress({ value, label }: { value: number; label: string }) {
    return (
        <div
            className="progress"
            role="progressbar"
            aria-label={label}
            aria-valuenow={value}
            aria-valuemin={0}
            aria-valuemax={100}
        >
            <div style={{ width: value + "%" }} />
        </div>
    );
}
/** Formats a percentage using the English interface locale. */
export const percent = (value: number) =>
    new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(value);
/** Changes the hash route and restores the page scroll position. */
export function navigate(path: string) {
    window.location.hash = path;
    window.scrollTo({ top: 0 });
}
/** Requests confirmation in a native modal with managed focus and Escape handling. */
export function ConfirmDialog({
    title,
    description,
    onCancel,
    onConfirm,
    action,
    cancelLabel = "Cancel",
}: {
    title: string;
    description: string;
    onCancel: () => void;
    onConfirm: () => void;
    action: string;
    cancelLabel?: string;
}) {
    const ref = useRef<HTMLDialogElement>(null);
    useEffect(() => {
        const dialog = ref.current!;
        dialog.showModal();
        return () => dialog.close();
    }, []);
    return (
        <dialog
            ref={ref}
            className="modal"
            aria-labelledby="confirm-title"
            aria-describedby="confirm-description"
            onCancel={(event) => {
                event.preventDefault();
                onCancel();
            }}
        >
            <h2 id="confirm-title">{title}</h2>
            <p id="confirm-description">{description}</p>
            <div className="modal-actions">
                <button autoFocus className="secondary" onClick={onCancel}>
                    {cancelLabel}
                </button>
                <button className="primary" onClick={onConfirm}>
                    {action}
                </button>
            </div>
        </dialog>
    );
}
