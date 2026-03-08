# 07 — Testear Utilidades (lib/)

---

## Por qué las utilidades son fáciles de testear

Las funciones en `lib/` son principalmente **funciones puras**: reciben argumentos,
devuelven un resultado, sin efectos secundarios ni estado externo. Se pueden
testear directamente sin montar componentes, sin Pinia y sin mocks.

---

## Testear `lib/utils.ts` — la función `cn()`

`cn()` combina clases Tailwind de forma inteligente: elimina duplicados y resuelve
conflictos (ej: `text-red-500` vs `text-blue-500` → gana el último).

```typescript
import { describe, it, expect } from "vitest";
import { cn } from "@/lib/utils";

describe("cn", () => {
    it("returns a single class string unchanged", () => {
        expect(cn("text-red-500")).toBe("text-red-500");
    });

    it("merges multiple class strings", () => {
        const result = cn("flex", "items-center", "gap-2");
        expect(result).toBe("flex items-center gap-2");
    });

    it("resolves conflicting Tailwind classes (last one wins)", () => {
        // text-red-500 vs text-blue-500: gana el último
        const result = cn("text-red-500", "text-blue-500");
        expect(result).toBe("text-blue-500");
    });

    it("handles conditional classes (falsy values are ignored)", () => {
        const isActive = false;
        const result = cn("base-class", isActive && "active-class");
        expect(result).toBe("base-class");
    });

    it("handles array syntax", () => {
        const result = cn(["flex", "gap-2"], "items-center");
        expect(result).toBe("flex gap-2 items-center");
    });

    it("handles object syntax (truthy keys are included)", () => {
        const result = cn({ "font-bold": true, "text-gray-500": false });
        expect(result).toBe("font-bold");
    });

    it("returns empty string for no inputs", () => {
        expect(cn()).toBe("");
    });
});
```

---

## Testear `lib/breadcrumbs.ts`

`lib/breadcrumbs.ts` exporta `BREADCRUMB_MAP`: un objeto que mapea nombres de ruta
a segmentos de breadcrumb. Testea que el mapa existe y contiene las rutas clave.

```typescript
import { describe, it, expect } from "vitest";
import { BREADCRUMB_MAP } from "@/lib/breadcrumbs";

describe("BREADCRUMB_MAP", () => {
    it("contains an entry for the home route", () => {
        expect(BREADCRUMB_MAP["home"]).toBeDefined();
        expect(BREADCRUMB_MAP["home"]).toHaveLength(1);
    });

    it("home route has a single segment with labelKey", () => {
        const segments = BREADCRUMB_MAP["home"];
        expect(segments[0]).toHaveProperty("labelKey");
    });

    it("wallets-create has two segments (parent + current)", () => {
        const segments = BREADCRUMB_MAP["wallets-create"];
        expect(segments).toHaveLength(2);
        // Primer segmento: enlace a la lista
        expect(segments[0]).toHaveProperty("routeName", "wallets");
        // Segundo segmento: página actual sin routeName
        expect(segments[1]).not.toHaveProperty("routeName");
    });

    it("wallet-detail uses dynamic label for the wallet name", () => {
        const segments = BREADCRUMB_MAP["wallet-detail"];
        expect(segments).toHaveLength(2);
        // El segundo segmento es dinámico (resuelto en runtime)
        expect(segments[1]).toHaveProperty("dynamic", true);
    });

    it("contains entries for all main domains", () => {
        const expectedRoutes = [
            "home", "wallets", "transactions", "budgets",
            "goals", "recurring", "settings", "notifications",
        ];
        for (const route of expectedRoutes) {
            expect(BREADCRUMB_MAP[route]).toBeDefined();
        }
    });
});
```

---

## Testear `lib/icon-catalog.ts`

Si existe un catálogo de iconos, testea que los iconos referenciados en el catálogo
son importables desde `lucide-vue-next`.

```typescript
import { describe, it, expect } from "vitest";
import * as LucideIcons from "lucide-vue-next";
import { ICON_CATALOG } from "@/lib/icon-catalog";

describe("ICON_CATALOG", () => {
    it("all icon names in the catalog exist in lucide-vue-next", () => {
        for (const [key, iconName] of Object.entries(ICON_CATALOG)) {
            expect(
                LucideIcons[iconName as keyof typeof LucideIcons],
                `Icon '${iconName}' (key: '${key}') not found in lucide-vue-next`
            ).toBeDefined();
        }
    });

    it("has an entry for each main transaction category", () => {
        const expectedCategories = [
            "housing", "transportation", "food", "health",
            "entertainment", "shopping", "education", "subscriptions",
            "savings", "debt", "gifts", "other",
        ];
        for (const category of expectedCategories) {
            expect(ICON_CATALOG[category]).toBeDefined();
        }
    });
});
```

---

## Testear interceptores de `lib/axios.ts`

`lib/axios.ts` configura una instancia de axios con interceptores para adjuntar tokens
y manejar errores 401. Para testear los interceptores sin hacer llamadas HTTP reales,
mockea `axios.create()` y captura los handlers que se registran.

```typescript
import { describe, it, expect, vi, beforeEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";

// Mock de axios ANTES de importar el módulo que lo usa
vi.mock("axios", () => {
    const interceptorRequest = { use: vi.fn() };
    const interceptorResponse = { use: vi.fn() };
    return {
        default: {
            create: vi.fn(() => ({
                interceptors: {
                    request: interceptorRequest,
                    response: interceptorResponse,
                },
                get: vi.fn(),
                post: vi.fn(),
            })),
        },
    };
});

import axios from "axios";

describe("axios interceptors setup", () => {
    beforeEach(() => {
        setActivePinia(createPinia());
    });

    it("registers a request interceptor on the axios instance", async () => {
        // Importar dinámicamente para que el mock esté activo
        await import("@/lib/axios");

        const mockAxiosInstance = axios.create();
        expect(mockAxiosInstance.interceptors.request.use).toHaveBeenCalled();
    });

    it("registers a response interceptor on the axios instance", async () => {
        await import("@/lib/axios");

        const mockAxiosInstance = axios.create();
        expect(mockAxiosInstance.interceptors.response.use).toHaveBeenCalled();
    });
});
```

**Nota:** Los interceptores de axios son difíciles de testear de forma granular en unit tests.
Para validar su comportamiento real (refresh del token, redirección a login en 401),
prefiere tests E2E que mockeen las respuestas HTTP con `page.route()`.
