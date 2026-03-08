# Testing Guide — Kakeibo App (Vue)

Guía de referencia permanente para escribir y entender los tests del frontend Vue de Kakeibo.
Dirigida a desarrolladores de cualquier nivel, con énfasis en el "por qué" antes del "cómo".

---

## Mapa de elementos → guía

| Elemento | Dónde se testea | Documento |
|----------|----------------|-----------|
| Stores Pinia (fetch, CRUD, computed) | `test/stores/*.spec.ts` | [03-stores.md](./03-stores.md) |
| Componentes reutilizables (cards, forms, badges) | `test/components/**/*.spec.ts` | [04-components.md](./04-components.md) |
| Vistas / páginas completas | E2E o test de view | [05-views.md](./05-views.md) |
| Composables (`useXxx`) | `test/composables/*.spec.ts` | [06-composables.md](./06-composables.md) |
| Funciones puras (`lib/utils`, `lib/breadcrumbs`) | `test/lib/*.spec.ts` | [07-utilities.md](./07-utilities.md) |
| Guards de navegación y rutas | `test/router/*.spec.ts` | [08-router.md](./08-router.md) |
| Flujos completos de usuario en navegador | `e2e/*.spec.ts` | [09-e2e.md](./09-e2e.md) |

---

## Árbol de decisión: ¿qué tipo de test escribo?

```
¿Qué quiero testear?
│
├── Lógica de datos / llamadas HTTP / estado reactivo
│   └── → Pinia store → [03-stores.md]
│
├── Renderizado / interacción UI
│   ├── ¿Es una pieza reutilizable (card, form, badge, lista)?
│   │   └── → Componente → [04-components.md]
│   └── ¿Es una página completa (view)?
│       ├── ¿Los subcomponentes ya están cubiertos?
│       │   └── → Test E2E → [09-e2e.md]
│       └── ¿Necesito testear lógica de orquestación aislada?
│           └── → View test → [05-views.md]
│
├── Lógica reactiva reutilizable (composable)
│   └── → [06-composables.md]
│
├── Funciones puras / helpers / utilidades
│   └── → [07-utilities.md]
│
├── Redirecciones y guards de autenticación
│   └── → [08-router.md]
│
└── Flujo completo del usuario en el navegador real
    └── → Playwright E2E → [09-e2e.md]
```

**Regla de oro**: si dudas entre un test de componente y un E2E, elige E2E para flujos que
involucran navegación o autenticación, y componente para comportamiento visual y de formulario.

---

## Comandos rápidos

```bash
# Ejecutar todos los unit tests (stores + components + composables + utils + router)
bun run app:test:unit

# Ejecutar unit tests en modo watch (desarrollo)
bun run app:test:watch

# Ejecutar todos los E2E tests (requiere servidor corriendo)
bun run app:test:e2e

# Ejecutar un solo archivo de test (unit)
cd src/Kakeibo.App && bunx vitest run test/stores/budgets.spec.ts

# Ejecutar tests que coincidan con un patrón (E2E)
bun run app:test:e2e --grep "budget"

# Modo UI interactivo para E2E
bun run app:test:e2e --ui

# Modo headed para ver el navegador durante E2E
bun run app:test:e2e --headed

# Debug interactivo E2E
bun run app:test:e2e --debug
```

---

## Convenciones de nombres y ubicación

| Tipo | Ubicación | Nombre |
|------|-----------|--------|
| Store test | `test/stores/{nombre}.spec.ts` | Igual que el store: `budgets.spec.ts` |
| Component test | `test/components/{dominio}/{Nombre}.spec.ts` | PascalCase igual que el componente |
| View test | `test/views/{dominio}/{Nombre}.spec.ts` | PascalCase igual que la vista |
| Composable test | `test/composables/{nombre}.spec.ts` | camelCase igual que el composable |
| Utility test | `test/lib/{nombre}.spec.ts` | camelCase igual que el archivo |
| Router test | `test/router/guards.spec.ts` | Descriptivo del guard o feature |
| E2E test | `e2e/{feature}.spec.ts` | camelCase, singular o plural según el dominio |

---

## Documentos de esta guía

| # | Documento | Contenido |
|---|-----------|-----------|
| — | README.md *(este archivo)* | Mapa, árbol de decisión, comandos, convenciones |
| 01 | [01-conceptos.md](./01-conceptos.md) | Por qué testeamos, tipos, pirámide |
| 02 | [02-configuracion.md](./02-configuracion.md) | Vitest + Playwright: setup y comandos |
| 03 | [03-stores.md](./03-stores.md) | Cómo testear Pinia stores (con código) |
| 04 | [04-components.md](./04-components.md) | Cómo testear componentes Vue (con código) |
| 05 | [05-views.md](./05-views.md) | Cómo testear vistas/páginas (con código) |
| 06 | [06-composables.md](./06-composables.md) | Cómo testear composables (con código) |
| 07 | [07-utilities.md](./07-utilities.md) | Cómo testear lib/utils (con código) |
| 08 | [08-router.md](./08-router.md) | Cómo testear guards y navegación (con código) |
| 09 | [09-e2e.md](./09-e2e.md) | Cómo escribir tests E2E con Playwright (con código) |
