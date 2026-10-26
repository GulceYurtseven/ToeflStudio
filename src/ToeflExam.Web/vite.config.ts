import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
    plugins: [react()],
    build: { outDir: "../ToeflExam.Api/wwwroot", emptyOutDir: false },
    server: {
        // Test trace files are transient and may be locked while Edge writes them.
        watch: { ignored: ["**/*test-results/**", "**/playwright-report/**"] },
        proxy: { "/api": { target: "https://localhost:7077", secure: false } },
    },
});
