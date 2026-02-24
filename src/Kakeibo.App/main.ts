import { createApp } from "vue";
import { createPinia } from "pinia";
import { createI18n } from "vue-i18n";
import App from "./App.vue";
import router from "./router";

// Import i18n messages
import en from "../locales/en.json";
import es from "../locales/es.json";

// Create i18n instance
const i18n = createI18n({
    legacy: false,
    locale: "en",
    fallbackLocale: "en",
    messages: {
        en,
        es,
    },
});

// Create app
const app = createApp(App);

// Use plugins
app.use(createPinia());
app.use(router);
app.use(i18n);

// Mount app
app.mount("#app");
