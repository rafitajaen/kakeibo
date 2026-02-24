# Kakeibo.App

Vue 3 Progressive Web App for the Kakeibo platform.

## Tech Stack

- **Vue 3** with Composition API (`<script setup>`)
- **TypeScript** (strict mode)
- **Pinia** for state management (setup stores)
- **Vue Router** for routing
- **Tailwind CSS v4** for styling
- **vue-i18n** for internationalization
- **Vite** for build tooling
- **Vitest** for unit tests
- **Playwright** for E2E tests

## Development

```bash
# Install dependencies
bun install

# Start dev server (http://localhost:5173)
bun run dev

# Type check
bun run typecheck

# Lint
bun run lint

# Format
bun run format

# Run unit tests
bun run test:unit

# Run E2E tests
bun run test:e2e

# Build for production
bun run build

# Preview production build
bun run preview
```

## Project Structure

```
src/Kakeibo.App/
├── src/
│   ├── components/     # Vue components
│   ├── stores/         # Pinia stores
│   ├── views/          # Page components
│   ├── router/         # Vue Router configuration
│   ├── assets/         # Static assets
│   ├── App.vue         # Root component
│   └── main.ts         # Application entry point
├── public/             # Public static files
├── locales/            # i18n translations
│   ├── en.json
│   └── es.json
├── test/               # Unit tests
├── e2e/                # E2E tests
├── index.html          # HTML shell
├── vite.config.ts      # Vite configuration
├── vitest.config.ts    # Vitest configuration
├── playwright.config.ts # Playwright configuration
├── tsconfig.json       # TypeScript configuration
├── Dockerfile          # Docker multi-stage build
└── nginx.conf          # Nginx configuration for production
```

## Docker

Build and run with Docker:

```bash
# Build image
docker build -t kakeibo-app .

# Run container
docker run -p 3000:80 kakeibo-app
```

## Conventions

- Use Composition API with `<script setup lang="ts">`
- Use Pinia setup stores: `defineStore('name', () => { ... })`
- All user-visible strings must use `t('key')` from vue-i18n
- Import from other directories using `@/` alias
- Use `bunx` instead of `npx` for CLI commands
- Follow SFC order: script → template → style scoped

## Contributing

See the root [CLAUDE.md](../../CLAUDE.md) for full project conventions and guidelines.
