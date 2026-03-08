import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import tailwindcss from "@tailwindcss/vite";
import { VitePWA } from "vite-plugin-pwa";
import { fileURLToPath, URL } from "node:url";
import { readFileSync } from "node:fs";

const pkg = JSON.parse(readFileSync(new URL("./package.json", import.meta.url), "utf-8")) as {
    version: string;
};

export default defineConfig({
    define: {
        "import.meta.env.VITE_APP_VERSION": JSON.stringify(pkg.version),
    },
    plugins: [
        vue(),
        tailwindcss(),
        VitePWA({
            // Use the existing custom sw.js that handles Web Push notifications.
            // injectManifest strategy injects the Workbox precache manifest into the custom SW.
            strategies: "injectManifest",
            srcDir: "public",
            filename: "sw.js",
            registerType: "autoUpdate",
            injectRegister: "auto",
            manifest: {
                name: "Kakeibo",
                short_name: "Kakeibo",
                description: "Personal finance and shared expense management",
                theme_color: "#09090b",
                background_color: "#09090b",
                display: "standalone",
                start_url: "/",
                icons: [
                    {
                        src: "/icon-192.png",
                        sizes: "192x192",
                        type: "image/png",
                    },
                    {
                        src: "/icon-512.png",
                        sizes: "512x512",
                        type: "image/png",
                        purpose: "any maskable",
                    },
                ],
            },
            devOptions: {
                enabled: false,
            },
        }),
    ],
    resolve: {
        alias: {
            "@": fileURLToPath(new URL(".", import.meta.url)),
        },
    },
    server: {
        port: 5173,
        host: true,
        proxy: {
            "/api": {
                target: "http://localhost:5000",
                changeOrigin: true,
            },
        },
    },
    build: {
        rollupOptions: {
            output: {
                manualChunks: {
                    "vendor-vue": ["vue", "vue-router", "pinia", "vue-i18n"],
                    "vendor-ui": [
                        "radix-vue",
                        "class-variance-authority",
                        "clsx",
                        "tailwind-merge",
                    ],
                    "vendor-form": ["vee-validate", "zod", "@vee-validate/zod"],
                },
            },
        },
    },
});
