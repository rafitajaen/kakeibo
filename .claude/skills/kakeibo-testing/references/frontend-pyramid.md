# Frontend Testing — Vitest + Playwright + Mobile + Email

Covers all frontend test levels: Vitest unit tests for Kakeibo.App and Kakeibo.Mobile,
Playwright E2E, and Bun tests for Kakeibo.Email.

---

## Global Vitest Setup

```typescript
// vitest.setup.ts
import { config } from '@vue/test-utils'
import { createI18n } from 'vue-i18n'
import en from '@/locales/en.json'
import es from '@/locales/es.json'

// Real i18n with actual locale files — catches missing keys during tests
export const i18n = createI18n({
    legacy: false,
    locale: 'es',
    messages: { en, es },
    missingWarn: true,
    fallbackWarn: true,
})

config.global.plugins = [i18n]
```

Always use real locale files, never hardcoded strings. This detects missing keys at test time.

---

## Component Tests (shadcn-vue + custom)

**Tools:** `@vue/test-utils` + Vitest.

**Location:** `test/components/{Name}.spec.ts`

**What to verify:** rendering, props, emits, slots, states (loading, error, empty).
Mock child components only when they introduce side effects (HTTP calls, timers).

```typescript
import { mount } from '@vue/test-utils'
import { createTestingPinia } from '@pinia/testing'
import WalletCard from '@/components/WalletCard.vue'

describe('WalletCard', () => {
    it('renders member name and email', () => {
        const wrapper = mount(WalletCard, {
            props: {
                member: { id: '1', name: 'Ana García', email: 'ana@test.com', status: 'active' }
            },
            global: { plugins: [createTestingPinia({ createSpy: vi.fn })] }
        })

        expect(wrapper.text()).toContain('Ana García')
        expect(wrapper.text()).toContain('ana@test.com')
    })

    it('emits edit event when edit button clicked', async () => {
        const wrapper = mount(WalletCard, {
            props: { member: { id: '42', name: 'Test', email: 'test@test.com', status: 'active' } },
            global: { plugins: [createTestingPinia({ createSpy: vi.fn })] }
        })

        await wrapper.find('[data-testid="edit-button"]').trigger('click')

        expect(wrapper.emitted('edit')).toBeTruthy()
        expect(wrapper.emitted('edit')![0]).toEqual(['42'])
    })

    it('shows disabled state when member is inactive', () => {
        const wrapper = mount(WalletCard, {
            props: { member: { id: '1', name: 'Test', email: 'test@test.com', status: 'inactive' } },
            global: { plugins: [createTestingPinia({ createSpy: vi.fn })] }
        })

        expect(wrapper.find('[data-testid="edit-button"]').attributes('disabled')).toBeDefined()
    })
})
```

---

## View / Page Tests

**What to verify:** component integration, data from store, programmatic navigation.
Inject initial state via `createTestingPinia()`.

```typescript
import { mount, flushPromises } from '@vue/test-utils'
import { createTestingPinia } from '@pinia/testing'
import { createRouter, createWebHistory } from 'vue-router'
import MembersView from '@/views/MembersView.vue'

describe('MembersView', () => {
    const router = createRouter({ history: createWebHistory(), routes: [] })

    it('displays member list from store', async () => {
        const wrapper = mount(MembersView, {
            global: {
                plugins: [
                    createTestingPinia({
                        createSpy: vi.fn,
                        initialState: {
                            members: {
                                members: [
                                    { id: '1', name: 'Ana García', email: 'ana@test.com' },
                                    { id: '2', name: 'Luis Pérez', email: 'luis@test.com' },
                                ],
                                isLoading: false,
                            }
                        }
                    }),
                    router,
                ]
            }
        })

        await flushPromises()

        expect(wrapper.findAll('[data-testid="member-row"]')).toHaveLength(2)
    })

    it('shows loading skeleton while fetching', () => {
        const wrapper = mount(MembersView, {
            global: {
                plugins: [
                    createTestingPinia({
                        createSpy: vi.fn,
                        initialState: { members: { isLoading: true, members: [] } }
                    }),
                    router,
                ]
            }
        })

        expect(wrapper.find('[data-testid="loading-skeleton"]').exists()).toBe(true)
    })
})
```

---

## Composable Tests

**Location:** `test/composables/use{Name}.spec.ts`

**What to verify:** reusable logic, side effects, cleanup in `onUnmounted`.

```typescript
import { mount } from '@vue/test-utils'
import { defineComponent } from 'vue'
import { usePagination } from '@/composables/usePagination'

// withSetup helper: mounts composable in a real Vue context
function withSetup<T>(composable: () => T): [T, ReturnType<typeof mount>] {
    let result!: T
    const TestComponent = defineComponent({
        setup() {
            result = composable()
            return {}
        },
        template: '<div />',
    })
    const wrapper = mount(TestComponent)
    return [result, wrapper]
}

describe('usePagination', () => {
    it('initializes with page 1 and default page size', () => {
        const [{ page, pageSize }] = withSetup(() => usePagination())

        expect(page.value).toBe(1)
        expect(pageSize.value).toBe(20)
    })

    it('nextPage increments page', () => {
        const [{ page, nextPage }] = withSetup(() => usePagination())

        nextPage()

        expect(page.value).toBe(2)
    })

    it('cleans up event listeners on unmount', () => {
        const removeListenerSpy = vi.fn()
        vi.spyOn(window, 'removeEventListener').mockImplementation(removeListenerSpy)

        const [, wrapper] = withSetup(() => usePagination())
        wrapper.unmount()

        expect(removeListenerSpy).toHaveBeenCalled()
    })
})
```

---

## Pinia Store Tests

**Location:** `test/stores/use{StoreName}.spec.ts`

**What to verify:** initial state, actions with mocked API, computed getters.
Each test gets a fresh Pinia — no shared state.

```typescript
import { setActivePinia, createPinia } from 'pinia'
import { useMembersStore } from '@/stores/members'

vi.mock('@/lib/api', () => ({
    membersApi: {
        getAll: vi.fn(),
        create: vi.fn(),
        delete: vi.fn(),
    }
}))

import { membersApi } from '@/lib/api'

describe('useMembersStore', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it('fetchMembers populates members from API', async () => {
        const mockMembers = [{ id: '1', name: 'Ana' }, { id: '2', name: 'Luis' }]
        vi.mocked(membersApi.getAll).mockResolvedValue(mockMembers)

        const store = useMembersStore()
        await store.fetchMembers()

        expect(store.members).toEqual(mockMembers)
        expect(store.isLoading).toBe(false)
    })

    it('fetchMembers sets error when API fails', async () => {
        vi.mocked(membersApi.getAll).mockRejectedValue(new Error('Network error'))

        const store = useMembersStore()
        await store.fetchMembers()

        expect(store.error).toBe('Network error')
        expect(store.members).toHaveLength(0)
    })

    it('activeMembers getter filters inactive members', () => {
        const store = useMembersStore()
        store.members = [
            { id: '1', name: 'Ana', status: 'active' },
            { id: '2', name: 'Luis', status: 'inactive' },
        ]

        expect(store.activeMembers).toHaveLength(1)
        expect(store.activeMembers[0].name).toBe('Ana')
    })
})
```

---

## Form Tests (VeeValidate + Zod)

**What to verify:** per-field validation, valid submit, invalid submit, i18n error messages.

```typescript
import { mount, flushPromises } from '@vue/test-utils'
import CreateWalletForm from '@/components/CreateWalletForm.vue'

describe('CreateWalletForm', () => {
    it('shows email validation error when email is empty', async () => {
        const wrapper = mount(CreateWalletForm, {
            global: { plugins: [i18n, createTestingPinia({ createSpy: vi.fn })] }
        })

        await wrapper.find('form').trigger('submit')
        await flushPromises()

        expect(wrapper.find('[data-testid="email-error"]').exists()).toBe(true)
    })

    it('calls onSubmit with valid data when form is filled correctly', async () => {
        const onSubmit = vi.fn()
        const wrapper = mount(CreateWalletForm, {
            props: { onSubmit },
            global: { plugins: [i18n, createTestingPinia({ createSpy: vi.fn })] }
        })

        await wrapper.find('[name="firstName"]').setValue('Ana')
        await wrapper.find('[name="lastName"]').setValue('García')
        await wrapper.find('[name="email"]').setValue('ana@test.com')
        await wrapper.find('form').trigger('submit')
        await flushPromises()

        expect(onSubmit).toHaveBeenCalledWith({
            firstName: 'Ana',
            lastName: 'García',
            email: 'ana@test.com',
        })
    })
})
```

---

## Router Guard Tests

**What to verify:** protected routes redirect, wrong-role routes block.

```typescript
import { createRouter, createWebHistory } from 'vue-router'
import { createTestingPinia } from '@pinia/testing'
import { setupRouterGuards } from '@/router/guards'

describe('Auth Router Guard', () => {
    it('redirects to login when user is not authenticated', async () => {
        const router = createRouter({ history: createWebHistory(), routes })
        const pinia = createTestingPinia({
            createSpy: vi.fn,
            initialState: { auth: { user: null, isAuthenticated: false } }
        })
        setupRouterGuards(router, pinia)

        await router.push('/admin/members')

        expect(router.currentRoute.value.path).toBe('/login')
    })

    it('allows access when user has required role', async () => {
        const router = createRouter({ history: createWebHistory(), routes })
        const pinia = createTestingPinia({
            createSpy: vi.fn,
            initialState: {
                auth: {
                    user: { id: '1', role: 'Admin' },
                    isAuthenticated: true
                }
            }
        })
        setupRouterGuards(router, pinia)

        await router.push('/admin/members')

        expect(router.currentRoute.value.path).toBe('/admin/members')
    })
})
```

---

## Axios Interceptor Tests

**What to verify:** Bearer token attached, automatic refresh on 401, logout on persistent 401.

```typescript
import axios from 'axios'
import MockAdapter from 'axios-mock-adapter'
import { setupAxiosInterceptors } from '@/lib/axios-interceptors'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from '@/stores/auth'

describe('Axios Interceptors', () => {
    let mock: MockAdapter

    beforeEach(() => {
        setActivePinia(createPinia())
        mock = new MockAdapter(axios)
        setupAxiosInterceptors(axios)
    })

    afterEach(() => mock.restore())

    it('attaches Bearer token to every request', async () => {
        const auth = useAuthStore()
        auth.accessToken = 'my-token'

        mock.onGet('/api/test').reply(200, { ok: true })

        await axios.get('/api/test')

        expect(mock.history.get[0].headers?.Authorization).toBe('Bearer my-token')
    })

    it('retries request with new token after 401', async () => {
        const auth = useAuthStore()
        auth.accessToken = 'expired-token'
        auth.refreshToken = vi.fn().mockResolvedValue('new-token')

        mock.onGet('/api/protected')
            .replyOnce(401)
            .onGet('/api/protected').reply(200, { data: 'ok' })

        const response = await axios.get('/api/protected')

        expect(auth.refreshToken).toHaveBeenCalledOnce()
        expect(response.data).toEqual({ data: 'ok' })
    })
})
```

---

## Mobile Tests — Capacitor Plugins

### Capacitor mock setup file

```typescript
// vitest.setup.mobile.ts
vi.mock('@capacitor/network', () => ({
    Network: {
        getStatus: vi.fn().mockResolvedValue({ connected: true, connectionType: 'wifi' }),
        addListener: vi.fn().mockResolvedValue({ remove: vi.fn() }),
    }
}))

vi.mock('@capacitor/preferences', () => ({
    Preferences: {
        get: vi.fn().mockResolvedValue({ value: null }),
        set: vi.fn().mockResolvedValue(undefined),
        remove: vi.fn().mockResolvedValue(undefined),
        clear: vi.fn().mockResolvedValue(undefined),
    }
}))

vi.mock('@capacitor/camera', () => ({
    Camera: {
        getPhoto: vi.fn().mockResolvedValue({ base64String: 'fake-base64', format: 'jpeg' }),
    }
}))
```

### Auth flow — token in Preferences (not cookie)

The mobile app uses `@capacitor/preferences` for the refresh token instead of HttpOnly cookies.
See KB-007 for the full reasoning.

```typescript
import { Preferences } from '@capacitor/preferences'
import { useMobileAuthStore } from '@/stores/mobileAuth'

describe('Mobile Auth Store', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.mocked(Preferences.get).mockResolvedValue({ value: null })
        vi.mocked(Preferences.set).mockResolvedValue(undefined)
    })

    it('loads refresh token from Preferences on startup', async () => {
        vi.mocked(Preferences.get).mockResolvedValue({ value: 'stored-refresh-token' })

        const store = useMobileAuthStore()
        await store.loadPersistedToken()

        expect(store.refreshToken).toBe('stored-refresh-token')
    })

    it('clears Preferences on logout', async () => {
        const store = useMobileAuthStore()
        await store.logout()

        expect(vi.mocked(Preferences.remove)).toHaveBeenCalledWith({ key: 'refreshToken' })
        expect(store.user).toBeNull()
        expect(store.accessToken).toBeNull()
    })
})
```

### Offline behavior

```typescript
describe('Offline Banner', () => {
    it('shows banner when network goes offline', async () => {
        vi.mocked(Network.addListener).mockImplementation((event, callback) => {
            if (event === 'networkStatusChange') {
                setTimeout(() => callback({ connected: false, connectionType: 'none' }), 0)
            }
            return Promise.resolve({ remove: vi.fn() })
        })

        const wrapper = mount(AppShell, {
            global: { plugins: [createTestingPinia({ createSpy: vi.fn })] }
        })

        await flushPromises()

        expect(wrapper.find('[data-testid="offline-banner"]').exists()).toBe(true)
    })
})
```

---

## E2E — Playwright

### Locator Strategy

Use locators in this order — most robust first, most fragile last:

```typescript
// 1. Role (ARIA) — survives HTML restructuring, semantically correct
await page.getByRole('button', { name: 'Subscribe' }).click()
await page.getByRole('heading', { level: 1 })
await page.getByRole('textbox', { name: 'Email' })

// 2. Test ID — explicit, immune to copy changes
await page.getByTestId('member-card').click()  // data-testid="member-card"

// 3. Label — good for form fields (tied to <label> element)
await page.getByLabel('Email address').fill('test@example.com')

// 4. Placeholder or alt text
await page.getByPlaceholder('Search members...')

// 5. Text — fragile, breaks on copy change. Use as last resort.
await page.getByText('Subscribe now')

// ❌ Never use XPath or CSS class selectors — brittle, break on refactoring
await page.locator('xpath=//div[@class="card"]/button')       // ❌
await page.locator('.WalletCard_container__xKq2p button')     // ❌
```

**Rule:** Always prefer `getByRole` — it tests accessibility and survives refactoring.
Add `data-testid` attributes only when no semantic role fits.

---

### Network Interception (`page.route()`)

Use `page.route()` to control API responses in E2E tests without a real backend:

```typescript
// Mock a successful response
await page.route('**/api/members', (route) =>
    route.fulfill({ json: { items: [], total: 0 } }))

// Simulate API error
await page.route('**/api/members', (route) =>
    route.fulfill({ status: 500, body: JSON.stringify({ error: 'Internal Server Error' }) }))

// Simulate slow network
await page.route('**/api/members', async (route) => {
    await new Promise(resolve => setTimeout(resolve, 2000))
    await route.fulfill({ status: 200, body: JSON.stringify([]) })
})

// Intercept and assert request payload
await page.route('**/api/members', async (route) => {
    const request = route.request()
    const body = JSON.parse(request.postData()!)
    expect(body.email).toBe('test@example.com')
    await route.fulfill({ json: { id: crypto.randomUUID() } })
})

// Pass through to real API (after intercepting for assertion)
await page.route('**/api/members', async (route) => {
    // Inspect then continue to real server
    console.log('Request:', route.request().url())
    await route.continue()
})
```

---

### Debugging Tools

```bash
# Trace viewer — run after a CI failure to replay the test step-by-step
npx playwright show-trace playwright-report/trace.zip

# Codegen — record interactions to scaffold test code
npx playwright codegen http://localhost:5173

# UI mode — visual interactive test runner (great for local development)
npx playwright test --ui

# Debug a single test with Playwright Inspector
npx playwright test --debug e2e/members/subscription.spec.ts

# Headed mode — watch the browser while the test runs
npx playwright test --headed
```

**Trace files are generated automatically** when `trace: 'on-first-retry'` is set in
`playwright.config.ts` (already configured). Download them from CI artifacts after a failure.

---

### CI Browser Caching (GitLab CI)

Use the official Playwright Docker image — it includes all browsers pre-installed.
Cache the browser binaries between runs to avoid re-downloading them.

```yaml
# .gitlab-ci.yml
quality:e2e:
  stage: quality
  image: mcr.microsoft.com/playwright:v1.50.0-noble
  rules:
    - if: $CI_PIPELINE_SOURCE == "merge_request_event"
  cache:
    key: playwright-${CI_COMMIT_REF_SLUG}
    fallback_keys:
      - playwright-main
    paths:
      - ~/.cache/ms-playwright/
  script:
    - bun install --frozen-lockfile
    - bun run app:test:e2e
  artifacts:
    when: on_failure
    paths:
      - playwright-report/
    expire_in: 7 days
```

**Notes:**
- Use `mcr.microsoft.com/playwright:v1.50.0-noble` — match version to `@playwright/test` in `package.json`
- The `fallback_keys: [playwright-main]` warms up the cache from `main` on first MR run
- Artifacts on failure upload the HTML report and trace files for debugging

#### Alternative: bun image with workspace-relative browser cache

The official Playwright image pre-installs all browsers, but requires pinning the image version and
re-pulling the full image when Playwright updates. When using `oven/bun` instead (e.g., to share the
`bun install` stage with other jobs), install browsers explicitly and set `PLAYWRIGHT_BROWSERS_PATH`
to a **workspace-relative path** — GitLab CI only caches paths inside `$CI_PROJECT_DIR`.

```yaml
# .gitlab-ci.yml — alternative: bun image + explicit browser install
quality:e2e:
  stage: quality
  image: oven/bun:1.3.8
  tags:
    - local   # Kakeibo self-hosted runner
  rules:
    - if: $CI_PIPELINE_SOURCE == "merge_request_event"
  variables:
    # Must be inside the workspace — GitLab CI cannot cache paths outside $CI_PROJECT_DIR
    PLAYWRIGHT_BROWSERS_PATH: "$CI_PROJECT_DIR/.playwright-browsers"
  cache:
    key: playwright-${CI_COMMIT_REF_SLUG}
    fallback_keys:
      - playwright-main
    paths:
      - .playwright-browsers/
  script:
    - cd sites/Kakeibo.App
    - bun install --frozen-lockfile
    # Install only chromium — headless-only for CI (no GPU required)
    - bunx playwright install --with-deps chromium
    - bun run app:test:e2e --project=chromium
  artifacts:
    when: on_failure
    paths:
      - sites/Kakeibo.App/playwright-report/
    expire_in: 7 days
```

**Key difference from the official image approach:**

| | Official Playwright image | bun image + explicit install |
|-|--------------------------|------------------------------|
| Image size | ~1.5 GB (browsers bundled) | Smaller (bun only) |
| Browser caching | Not needed (already bundled) | Required — use `PLAYWRIGHT_BROWSERS_PATH` inside workspace |
| Browser updates | Tied to image version pin | Independent of image |
| GitLab cache path | `~/.cache/ms-playwright/` ⚠️ outside workspace | `.playwright-browsers/` ✅ inside workspace |

> ⚠️ `~/.cache/ms-playwright/` is **outside** `$CI_PROJECT_DIR` on most runners. GitLab CI's
> `cache.paths` only works reliably for paths inside the project directory. Always use
> `PLAYWRIGHT_BROWSERS_PATH: "$CI_PROJECT_DIR/.playwright-browsers"` when caching browsers.

---

### Architecture decision: real API vs mocked API

| Mode | When to use |
|------|-------------|
| **Full stack** (real API + Docker) | Critical end-to-end flows, real JWT/cookies/sessions, data that persists across screens |
| **Mocked API** (`page.route()`) | Fast UI tests, error states, empty states, edge cases impossible in real DB |

### Authentication — No manual login per test

**Strategy 1 — Storage State (recommended for Kakeibo.App):**

```typescript
// global-setup.ts — runs once before all tests
import { chromium } from '@playwright/test'

async function globalSetup() {
    const browser = await chromium.launch()
    const page = await browser.newPage()

    await page.goto('/login')
    await page.getByLabel('Email').fill('test-admin@example.com')
    await page.getByLabel('Password').fill('Test#12345Abc')
    await page.getByRole('button', { name: 'Sign in' }).click()
    await page.waitForURL('/dashboard')

    await page.context().storageState({ path: 'playwright/.auth/admin.json' })
    await browser.close()
}

export default globalSetup
```

```typescript
// playwright.config.ts
export default defineConfig({
    globalSetup: './global-setup.ts',
    use: {
        baseURL: 'http://localhost:5173',
        trace: 'on-first-retry',
        screenshot: 'only-on-failure',
    },
    projects: [
        { name: 'setup', testMatch: /.*\.setup\.ts/ },
        {
            name: 'admin',
            use: { storageState: 'playwright/.auth/admin.json' },
            dependencies: ['setup'],
        },
        {
            name: 'member',
            use: { storageState: 'playwright/.auth/member.json' },
            dependencies: ['setup'],
        },
        {
            name: 'unauthenticated',
            testMatch: /auth\/(login|register)\.spec\.ts/,
        },
    ],
})
```

**Strategy 2 — Auth Fixture (for multiple roles):**

```typescript
// fixtures/auth.ts
import { test as base } from '@playwright/test'

export const test = base.extend<{ adminPage: Page; memberPage: Page }>({
    adminPage: async ({ page, request }, use) => {
        await request.post('/api/auth/register', {
            data: { email: 'e2e-admin@test.com', password: 'Test#12345Abc', username: 'e2eadmin' }
        }).catch(() => {})

        const response = await request.post('/api/auth/login', {
            data: { email: 'e2e-admin@test.com', password: 'Test#12345Abc' }
        })
        const { accessToken } = await response.json()

        await page.addInitScript((token: string) => {
            localStorage.setItem('accessToken', token)
        }, accessToken)

        await page.goto('/')
        await use(page)
    },
})
```

### E2E folder organization

```
e2e/
  auth/
    login.spec.ts               → login, logout, remember-me, token refresh
    register.spec.ts            → registration, email verification, duplicate
    password-recovery.spec.ts   → request reset, use token, expired token
  members/
    subscription.spec.ts        → subscribe, renew, cancel
    profile.spec.ts             → edit profile, change password
  padel/
    booking.spec.ts             → book court, cancel, check availability
  admin/
    users.spec.ts               → CRUD users, assign roles
    roles.spec.ts               → CRUD roles, permissions, SuperAdmin protected
  fixtures/
    auth.ts                     → reusable auth fixtures
    data.ts                     → test data factories
```

### E2E test examples

```typescript
// Auth flow
test('login with correct credentials redirects to dashboard', async ({ page }) => {
    await page.goto('/login')
    await page.getByLabel('Email').fill('admin@test.com')
    await page.getByLabel('Password').fill('Test#12345Abc')
    await page.getByRole('button', { name: 'Sign in' }).click()

    await expect(page).toHaveURL('/dashboard')
})

test('accessing protected route without login redirects to login', async ({ page }) => {
    await page.goto('/admin/members')
    await expect(page).toHaveURL('/login')
})

// Happy path CRUD
test('creates, edits, and deletes a member', async ({ adminPage: page }) => {
    const email = `e2e-${Date.now()}@test.com`

    await page.goto('/admin/members')
    await page.getByRole('button', { name: 'New member' }).click()
    await page.getByLabel('First name').fill('Ana')
    await page.getByLabel('Last name').fill('García')
    await page.getByLabel('Email').fill(email)
    await page.getByRole('button', { name: 'Create' }).click()
    await expect(page.getByText('Wallet created successfully')).toBeVisible()

    await page.goto('/admin/members')
    await expect(page.getByText(email)).toBeVisible()
})

// Error states with mocked API
test('shows error banner when API returns 500', async ({ page }) => {
    await page.route('/api/members', route =>
        route.fulfill({ status: 500, body: JSON.stringify({ error: 'Internal Server Error' }) })
    )

    await page.goto('/admin/members')

    await expect(page.getByRole('alert')).toBeVisible()
})

test('shows empty state when member list is empty', async ({ page }) => {
    await page.route('/api/members', route =>
        route.fulfill({ status: 200, body: JSON.stringify([]) })
    )

    await page.goto('/admin/members')

    await expect(page.getByTestId('empty-state')).toBeVisible()
})

// Slow network
test('handles slow network gracefully', async ({ page }) => {
    await page.route('/api/members', async route => {
        await new Promise(resolve => setTimeout(resolve, 2000))
        await route.fulfill({ status: 200, body: JSON.stringify([]) })
    })

    await page.goto('/admin/members')

    await expect(page.getByTestId('loading-skeleton')).toBeVisible()
    await expect(page.getByTestId('loading-skeleton')).not.toBeVisible({ timeout: 5000 })
})
```

---

## Email Renderer Tests (Kakeibo.Email)

**Tool:** Bun test runner

**Script:** `bun run email:test`

```typescript
import { describe, it, expect } from 'bun:test'
import { renderEmail } from '../src/renderer'

describe('WelcomeEmail', () => {
    it('renders member name in subject', async () => {
        const result = await renderEmail('welcome', { memberName: 'Ana García' })

        expect(result.subject).toContain('Ana García')
        expect(result.html).toContain('Ana García')
    })

    it('renders without throwing for minimal payload', async () => {
        await expect(
            renderEmail('welcome', { memberName: 'Test' })
        ).resolves.toBeDefined()
    })
})
```

---

## Frontend Test Data Fixtures

```typescript
// TypeScript factory helper
function createWalletFixture(overrides: Partial<Member> = {}): Member {
    return {
        id: crypto.randomUUID(),
        name: 'Ana García',
        email: 'ana@test.com',
        status: 'active',
        ...overrides,
    }
}

// Usage
const inactiveMember = createWalletFixture({ status: 'inactive' })
const memberWithoutEmail = createWalletFixture({ email: '' })
```
