import React from "react";
import { createRoot } from "react-dom/client";
import { App } from "./App";
import "./styles.css";
import "./accessibility.css";
import "./studios.css";
import "./speaking.css";
// Shared production UI overrides follow the base studio styles.
import "./community.css";

class ErrorBoundary extends React.Component<
    { children: React.ReactNode },
    { failed: boolean }
> {
    state = { failed: false };
    /** Switches the error boundary to its recovery view after a rendering failure. */
    static getDerivedStateFromError() {
        return { failed: true };
    }
    /** Displays the application or a safe reload prompt after a rendering error. */
    render() {
        return this.state.failed ? (
            <main className="fatal">
                <h1>Something went wrong.</h1>
                <p>Your draft remains in this tab. You can reload the page. </p>
                <button onClick={() => location.reload()}>Reload page </button>
            </main>
        ) : (
            this.props.children
        );
    }
}
// Mount the application with a recovery boundary for unexpected render failures.
createRoot(document.getElementById("root")!).render(
    <React.StrictMode>
        <ErrorBoundary>
            <App />
        </ErrorBoundary>
    </React.StrictMode>,
);
