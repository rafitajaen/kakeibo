# Email Rendering Service

Standalone HTTP microservice for rendering and delivering transactional email templates.

---

## Tech Stack

| Component | Description |
|-----------|-------------|
| React Email | Email template rendering with React components |
| Hono | Micro HTTP server for email rendering API |
| Bun | Runtime for email renderer service |
| oxlint | Lint |
| oxfmt | Format |

## Prohibited Technologies

| Technology | Reason |
|------------|--------|
| Razor (email templates) | Use React Email for templates |
| mjml | Use React Email for templates |
| Biome | Use oxlint + oxfmt |
