# 01 — Conceptos de Testing

---

## ¿Por qué testeamos?

Testear no es burocracia: es la forma más eficiente de ganar confianza en el código.
Tres razones concretas para este proyecto:

**Confianza al cambiar código.** Kakeibo es un MVP que sigue creciendo. Sin tests,
cada cambio es una apuesta: ¿rompí algo? Con tests, lo sabes en segundos.

**Prevención de regresiones.** Un bug que se corrige sin test volverá a aparecer.
Un bug corregido con test queda documentado y bloqueado para siempre.

**Documentación viva.** Un test bien escrito explica qué debe hacer el código.
Es más fiable que un comentario porque falla cuando la realidad cambia.

---

## Los tres tipos de tests en este proyecto

### Unit tests (Vitest)

Testean una sola unidad de código en completo aislamiento: un store, un componente,
un composable, una función. Las dependencias externas (HTTP, router, otros stores) se
reemplazan por mocks controlados.

Se ejecutan con `bun run app:test:unit`. Son rápidos (milisegundos) y se pueden
ejecutar cientos de veces al día sin problema.

**Están en:** `src/Kakeibo.App/test/`

### Component tests (Vitest + Vue Test Utils)

Son un subconjunto de los unit tests. Montan un componente Vue en un DOM simulado
(jsdom) y verifican su renderizado, sus props, sus eventos emitidos y su comportamiento
ante la interacción del usuario. No involucran un navegador real.

También se ejecutan con `bun run app:test:unit` (Vitest los recoge junto a los demás).

**Están en:** `src/Kakeibo.App/test/components/`

### E2E tests (Playwright)

Testean la aplicación completa desde la perspectiva del usuario: abren un navegador real
(Chromium), navegan a URLs, hacen clic en botones, rellenan formularios y verifican lo
que aparece en pantalla. Las llamadas HTTP se interceptan con `page.route()` para no
depender de una API en producción.

Se ejecutan con `bun run app:test:e2e`. Son más lentos (segundos por test) y se
reservan para los flujos más críticos.

**Están en:** `src/Kakeibo.App/e2e/`

---

## La pirámide de tests

```
        /\
       /E2E\         — pocos, lentos, flujos críticos de usuario
      /------\
     /Component\     — algunos, verifican renderizado y comportamiento UI
    /------------\
   /  Unit/Store  \  — muchos, rápidos, lógica de negocio aislada
  /________________\
```

**Muchos unit tests** porque son baratos de escribir y ejecutar.
**Algunos component tests** porque verifican la interfaz de usuario sin un navegador real.
**Pocos E2E tests** porque son los más lentos y los más frágiles ante cambios de UI.

---

## Cuándo es suficiente cada tipo

| Situación | Tipo recomendado |
|-----------|----------------|
| Lógica de un store: fetch, CRUD, computed | Unit (store test) |
| Componente con props, formulario, eventos | Component test |
| Página completa con navegación | E2E |
| Función pura sin efectos | Unit (utility test) |
| Guard de autenticación | Router test o E2E |
| Flujo registro → onboarding → dashboard | E2E |

---

## Qué NO testear

**Librerías externas.** No testees que `axios.get()` funciona: eso es responsabilidad
de los maintainers de axios. Mockea axios y testea cómo tu código reacciona a su respuesta.

**Internals de shadcn-vue.** No testees que un `<Select>` de shadcn abre su dropdown:
eso ya lo testea shadcn. Testea que tu componente pasa el valor correcto y reacciona
al cambio.

**Estilos y clases CSS.** Los tests de estilos son frágiles y no aportan valor.
Si una clase cambia, el test falla aunque la UI siga siendo correcta visualmente.

**Detalles de implementación.** No testees el nombre de una variable interna de un store.
Testea el comportamiento observable: qué devuelve, qué muta, qué llama.
