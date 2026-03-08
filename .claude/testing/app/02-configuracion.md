# 02 — Configuración de Vitest y Playwright

---

## Vitest (`src/Kakeibo.App/vitest.config.ts`)

```typescript
import { defineConfig } from "vitest/config";
import vue from "@vitejs/plugin-vue";
import { fileURLToPath, URL } from "node:url";

export default defineConfig({
    plugins: [vue()],           // Necesario para que Vitest entienda SFCs (.vue)
    test: {
        globals: true,          // describe, it, expect disponibles sin import
        environment: "jsdom",   // DOM simulado (no happy-dom) para montar componentes
        exclude: [              // Los E2E no son unit tests: se excluyen aquí
            "**/node_modules/**",
            "**/dist/**",
            "**/e2e/**",
        ],
        passWithNoTests: true,  // No falla si no hay tests (útil durante desarrollo)
    },
    resolve: {
        alias: {
            "@": fileURLToPath(new URL(".", import.meta.url)),  // @/ → raíz del proyecto
        },
    },
});
```

**Por qué `globals: true`:** Permite escribir `describe(...)` e `it(...)` directamente
sin importarlos. Menos boilerplate en cada archivo de test.

**Por qué `jsdom`:** Es el entorno más compatible para renderizar componentes Vue
con `@vue/test-utils`. No es un navegador real, pero soporta suficiente DOM API
para los tests de UI que hacemos.

**Por qué excluir `e2e/`:** Los tests de Playwright usan una API diferente (`test` de
`@playwright/test`) e incompatible con Vitest. Excluirlos evita errores de colisión.

---

## Playwright (`src/Kakeibo.App/playwright.config.ts`)

```typescript
import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
    testDir: "./e2e",               // Solo busca specs en la carpeta e2e/
    fullyParallel: true,            // Todos los tests en paralelo (por defecto)
    forbidOnly: !!process.env.CI,   // test.only() falla en CI (evita commits accidentales)
    retries: process.env.CI ? 2 : 0, // 2 reintentos en CI, 0 en local
    workers: process.env.CI ? 1 : undefined, // CI: serie; local: paralelo
    reporter: "html",               // Genera reporte HTML navegable
    use: {
        baseURL: "http://localhost:5173",  // URL base del dev server de Vite
        trace: "on-first-retry",          // Captura trace si el test falla y se reintenta
    },
    projects: [
        {
            name: "chromium",
            use: { ...devices["Desktop Chrome"] },  // Solo Chromium en esta fase
        },
    ],
    webServer: {
        command: "bun run dev",                    // Arranca el dev server automáticamente
        url: "http://localhost:5173",
        reuseExistingServer: !process.env.CI,      // Reutiliza servidor si ya está corriendo (local)
    },
});
```

**Por qué `workers: 1` en CI:** Las GitHub Actions tienen recursos limitados.
Tests paralelos pueden saturar la memoria y producir falsos negativos.

**Por qué `trace: "on-first-retry"`:** Un trace captura screenshots, red, consola y DOM
del test fallido. Se activa solo en el reintento para no penalizar el caso exitoso.

**Por qué un solo browser (Chromium):** En el MVP priorizamos velocidad de desarrollo.
Añadir Firefox/Safari es sencillo cuando sea necesario.

---

## Comandos disponibles

| Comando | Cuándo usarlo |
|---------|--------------|
| `bun run app:test:unit` | Antes de cada commit; en CI siempre |
| `bun run app:test:watch` | Durante desarrollo para ver feedback inmediato |
| `bun run app:test:e2e` | Antes de merge a main; validar flujos críticos |
| `bun run app:test:e2e --ui` | Depurar E2E de forma visual e interactiva |
| `bun run app:test:e2e --headed` | Ver el navegador durante la ejecución |
| `bun run app:test:e2e --debug` | Pausar en cada paso para inspeccionar estado |
| `bun run app:test:e2e --grep "budget"` | Ejecutar solo tests que coinciden con el patrón |

---

## Dónde colocar cada tipo de test

```
src/Kakeibo.App/
├── test/
│   ├── stores/                      # Un .spec.ts por store
│   │   └── budgets.spec.ts
│   ├── components/                  # Subdirectorio por dominio, un .spec.ts por componente
│   │   ├── budgets/
│   │   │   └── BudgetForm.spec.ts
│   │   └── wallets/
│   │       └── WalletCard.spec.ts
│   ├── composables/                 # Un .spec.ts por composable (crear si no existe)
│   │   └── useCurrencyFormat.spec.ts
│   ├── lib/                         # Un .spec.ts por archivo de utilidades (crear si no existe)
│   │   └── utils.spec.ts
│   └── router/                      # Guards y navegación (crear si no existe)
│       └── guards.spec.ts
└── e2e/
    └── budgets.spec.ts              # Un .spec.ts por dominio/flujo
```

---

## Cómo ejecutar un solo archivo o un solo test

```bash
# Ejecutar un único archivo de test (unit)
cd src/Kakeibo.App && bunx vitest run test/stores/budgets.spec.ts

# Ejecutar un test específico por nombre (unit)
cd src/Kakeibo.App && bunx vitest run --reporter=verbose -t "fetchBudgets populates"

# Ejecutar un único spec E2E
bun run app:test:e2e e2e/budgets.spec.ts

# Ejecutar un único test E2E por nombre
bun run app:test:e2e --grep "unauthenticated user is redirected"
```
