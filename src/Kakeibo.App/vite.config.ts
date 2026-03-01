import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import tailwindcss from "@tailwindcss/vite";
import { fileURLToPath, URL } from "node:url";

export default defineConfig({
    plugins: [vue(), tailwindcss()],
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
