# Kakeibo App

Vue.js PWA frontend for the Kakeibo personal finance platform.

---

## Tech Stack

| Component | Description |
|-----------|-------------|
| Vue.js | Framework with Composition API |
| Vite | Build tool with HMR |
| TypeScript | Strict mode required |
| Pinia | State management |
| Axios | HTTP client |
| Vue Router | Routing |
| shadcn-vue | Accessible UI components |
| Tailwind CSS v4 | Utility-first styles, no configuration file |
| lucide-vue-next | Icons |
| VeeValidate + Zod | Form validation |
| date-fns | Date manipulation |
| Axios (interceptors) + Pinia | Authentication state management and automatic token refresh |
| Radix UI | Charts |
| Playwright | E2E tests |
| Vitest | Unit tests |
| i18n | Internationalization |
| Bun | Package manager |
| oxlint | Lint |
| oxfmt | Format |
| .env typed | Environment variables |

## Prohibited Technologies

| Technology | Reason |
|------------|--------|
| Chart.js | Use Radix UI for charts |
| Webpack | Use Vite instead |
| dayjs | Use date-fns instead |
| datejs | Use date-fns instead |
| frappe-ui | Use shadcn-vue instead |
| Biome | Use oxlint + oxfmt |
| @hugeicons/vue / @hugeicons/core-free-icons | Use lucide-vue-next instead |
