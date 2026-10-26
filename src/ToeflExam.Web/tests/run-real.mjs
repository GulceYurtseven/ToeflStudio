// Isolated SQL database + real Kestrel + real browser. No external AI requests.
import { spawn, execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";
const web = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const root = resolve(web, "../..");
const database = "ToeflExam_UiTests_" + randomUUID().replaceAll("-", "");
const configuration =
    process.env.TOEFL_UI_CONFIGURATION === "Release" ? "Release" : "Debug";
const env = {
    ...process.env,
    ASPNETCORE_ENVIRONMENT: "UiTesting",
    ASPNETCORE_URLS: "http://127.0.0.1:5187",
    Gemini__ApiKey: "",
    ConnectionStrings__ToeflExam:
        "Server=localhost;Database=" +
        database +
        ";Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=5",
};
let server;
try {
    // Refuse to attach tests to an unrelated process already listening on the test port.
    try {
        await fetch("http://127.0.0.1:5187/health/live", {
            signal: AbortSignal.timeout(1000),
        });
        throw new Error("Test port 5187 is in use.");
    } catch (error) {
        if (error.message === "Test port 5187 is in use.") throw error;
    }
    execFileSync(
        "dotnet",
        [
            "ef",
            "database",
            "update",
            "--project",
            "src/ToeflExam.Infrastructure",
            "--startup-project",
            "src/ToeflExam.Infrastructure",
            "--no-build",
            "--configuration",
            configuration,
        ],
        { cwd: root, env, stdio: "inherit", windowsHide: true },
    );
    server = spawn(
        "dotnet",
        [
            resolve(
                root,
                `src/ToeflExam.Api/bin/${configuration}/net10.0/ToeflExam.Api.dll`,
            ),
        ],
        {
            cwd: resolve(root, "src/ToeflExam.Api"),
            env,
            stdio: ["ignore", "ignore", "inherit"],
            windowsHide: true,
        },
    );
    let ready = false;
    for (let i = 0; i < 60; i++) {
        try {
            if ((await fetch("http://127.0.0.1:5187/health/live")).ok) {
                ready = true;
                break;
            }
        } catch {}
        if (server.exitCode !== null)
            throw new Error("Test API exited unexpectedly.");
        await new Promise((resolve) => setTimeout(resolve, 500));
    }
    if (!ready) throw new Error("Test API did not become ready.");
    execFileSync(
        process.execPath,
        [
            resolve(web, "node_modules/@playwright/test/cli.js"),
            "test",
            "--config",
            "playwright.real.config.ts",
            ...process.argv.slice(2),
        ],
        { cwd: web, env, stdio: "inherit", windowsHide: true },
    );
} finally {
    if (server && server.exitCode === null) {
        const stopped = new Promise((resolve) => server.once("exit", resolve));
        server.kill();
        await stopped;
    }
    if (!/^ToeflExam_UiTests_[a-f0-9]{32}$/.test(database))
        throw new Error("Unsafe test database target.");
    execFileSync(
        "sqlcmd",
        [
            "-S",
            "localhost",
            "-d",
            "master",
            "-E",
            "-C",
            "-b",
            "-Q",
            "IF DB_ID(N'" +
                database +
                "') IS NOT NULL BEGIN ALTER DATABASE [" +
                database +
                "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" +
                database +
                "]; END",
        ],
        { cwd: root, stdio: "inherit", windowsHide: true },
    );
}
