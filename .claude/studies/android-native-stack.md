# Android Native Stack Study — Kakeibo Android App

> Comprehensive technology selection for a **native Android** application replicating
> the full Kakeibo feature set with a level of polish comparable to modern finance apps
> like Wilo. Covers every layer: UI, state, navigation, theming, charts, icons, adaptive
> layouts, networking, storage, and testing. Every decision is justified and a single
> recommended option is given per concern.
>
> **Scope:** Android-only. No iOS, no Kotlin Multiplatform. Native first.

---

## 1. Scope

The Android app must replicate all Kakeibo screens:

- **Auth**: login, register, email verification, forgot password, reset password
- **Wallets**: list, detail, create, edit, shared wallet management, invite members
- **Transactions**: list per wallet, record, edit; with file attachments
- **Categories**: list (system + custom), create, edit
- **Budgets**: list, create, edit; with progress visualization
- **Goals**: list, create, edit; with milestone tracking
- **Recurring**: list, create, edit; with 30/90-day forecast
- **Collaboration**: friends, friend requests, shared wallet members, settlements, debt view
- **Notifications**: list, mark-as-read, push subscription, preferences
- **Activity Feed**: paginated audit log with filters
- **Dashboard**: balance overview, area chart (7d/30d/90d), metric cards, budget/goal summaries, recent transactions, quick actions
- **Settings**: profile, password, sessions, display preferences, import/export
- **Admin**: user management, platform policy (super-admin only)
- **Onboarding**: step wizard with optional seed data

**Non-functional requirements:**
- Simple to understand and extend for a single developer
- Scalable folder structure (can grow to 10+ features without restructuring)
- Excellent theming: light, dark, and system-default; Material You dynamic color on Android 12+
- Modern, beautiful charts (no legacy View-based wrappers)
- Good icon library with consistent visual language
- Works correctly on phone (360–414dp) and tablet (600–1024dp)
- Android 8.0+ (API 26) minimum target; Android 12+ (API 31) recommended features

---

## 2. Language & Runtime

**Kotlin 2.x + Jetpack Compose (BOM-managed).**

No alternatives exist for a serious new native Android project:
- Kotlin is the officially supported language since 2017. Java is legacy.
- Jetpack Compose is the officially recommended UI toolkit since 2021. XML Views are legacy for new projects.
- Compose removes the 1:1 View/ViewHolder ceremony, enables declarative state-driven UI, and integrates directly with Kotlin coroutines and Flow.
- Kotlin 2.x brings the K2 compiler (faster build times, better type inference, improved smart cast).

**Minimum SDK:** API 26 (Android 8.0) — covers 97%+ of active Android devices.
**Target SDK:** Latest stable API (API 35 at time of writing).

---

## 3. Architecture

### 3.1 Pattern: MVVM + Unidirectional Data Flow (UDF) + Feature Vertical Slices

Mirror the API's screaming architecture folder structure. Each feature is self-contained.

```
app/
  src/main/
    java/com/kakeibo/
      core/
        api/                  # HTTP client, interceptors, ApiResult<T>
        auth/                 # AuthRepository, token storage
        navigation/           # Type-safe routes, NavHost
        theme/                # MaterialTheme, color schemes, typography
        di/                   # Hilt AppModule, NetworkModule
        utils/                # CurrencyFormatter, DateFormatter, extensions
      features/
        auth/
          data/               # AuthRepository, AuthApi (Retrofit)
          domain/             # User, LoginRequest, RegisterRequest (data classes)
          presentation/
            LoginScreen.kt
            RegisterScreen.kt
            AuthViewModel.kt
            AuthUiState.kt    # sealed interface
        dashboard/
          data/
          domain/
          presentation/
        wallets/
          data/
          domain/
          presentation/
        transactions/
        categories/
        budgets/
        goals/
        recurring/
        notifications/
        friends/
        activity/
        settings/
        admin/
        onboarding/
```

### 3.2 Layering per Feature

1. **domain/**: Pure Kotlin data classes. No Android/Compose imports. Serializable.
2. **data/**: Repository + Retrofit API interface. Returns `Result<T>` or `Flow<T>`.
3. **presentation/**: `ViewModel` + `UiState` + Composable screens and components.

This is "Clean Architecture lite" — enough separation to test each layer independently,
simple enough for a single developer to navigate without ceremony.

### 3.3 UiState Pattern (UDF)

Every screen has an associated sealed class/interface representing all possible UI states:

```kotlin
// AuthUiState.kt
sealed interface LoginUiState {
    data object Idle : LoginUiState
    data object Loading : LoginUiState
    data class Success(val userId: String) : LoginUiState
    data class Error(val message: String) : LoginUiState
}

// AuthViewModel.kt
@HiltViewModel
class AuthViewModel @Inject constructor(
    private val authRepository: AuthRepository,
) : ViewModel() {
    private val _uiState = MutableStateFlow<LoginUiState>(LoginUiState.Idle)
    val uiState: StateFlow<LoginUiState> = _uiState.asStateFlow()

    fun login(email: String, password: String) {
        viewModelScope.launch {
            _uiState.value = LoginUiState.Loading
            authRepository.login(email, password)
                .onSuccess { user -> _uiState.value = LoginUiState.Success(user.id) }
                .onFailure { e -> _uiState.value = LoginUiState.Error(e.message ?: "Unknown error") }
        }
    }
}

// LoginScreen.kt
@Composable
fun LoginScreen(viewModel: AuthViewModel = hiltViewModel()) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    // render based on uiState
}
```

This pattern prevents:
- Toast/dialog reappearing on screen rotation (state is lifecycle-aware)
- Double navigation bugs (Navigation events go through a Channel, consumed once)
- Silent failures (every state is explicit and typed)

---

## 4. State Management — ViewModel + StateFlow

**No external state management library is needed.** Android's official solution is complete:

- `ViewModel` (Jetpack): Survives configuration changes (rotation). Tied to the screen lifecycle.
- `StateFlow`: Cold, lifecycle-aware, replays last value on collection. The standard for UI state.
- `SharedFlow` / `Channel`: For one-shot events (navigation commands, snackbars) that must not replay.
- `viewModelScope`: Structured concurrency tied to ViewModel lifecycle — auto-cancelled on clear.
- `collectAsStateWithLifecycle()`: Compose extension that stops collecting in background (saves battery).

**Why not alternatives:**
- **Redux / Orbit MVI / Circuit (Slack)**: Add significant ceremony (actions, reducers, middlewares) for the same outcome. Justified only for large multi-team projects.
- **Molecule**: Converts Compose's snapshot system into Flow. Niche use case. Adds complexity.
- **GetX / MobX for Android**: Not idiomatic. Community-maintained. No official support.

The MVVM + StateFlow combination is the pattern shown in every official Android developer guide,
Compose codelab, and Google I/O talk. It maps cleanly to Pinia's `defineStore` setup function style:
`ref()` → `MutableStateFlow`, `computed()` → derived `StateFlow`, actions → ViewModel functions.

---

## 5. Dependency Injection — Hilt

**Library:** `hilt-android` + `hilt-navigation-compose`

Hilt is Google's official DI framework for Android, built on Dagger 2.

**Why Hilt over Koin:**
- **Compile-time safety**: Hilt validates the entire dependency graph at build time. Koin is runtime — missing dependencies crash the app on first use, not at compile time.
- **IDE support**: Android Studio understands Hilt annotations. Gutter icons show where things are injected. Koin has no equivalent tooling.
- **Official**: Hilt is maintained by Google, documented in all Android developer guides. Koin is a community project.
- **Compose integration**: `hiltViewModel()` and `@HiltViewModel` work out of the box with Navigation Compose.
- **Testing**: `HiltAndroidTest` + `@TestInstallIn` give surgical control over test DI graphs.

**Why not Koin:**
Despite Koin's simpler DSL, the runtime nature means a misconfigured module silently breaks at runtime.
For a production finance app, compile-time guarantees are worth the annotation overhead.

```kotlin
// NetworkModule.kt
@Module
@InstallIn(SingletonComponent::class)
object NetworkModule {
    @Provides @Singleton
    fun provideRetrofit(authInterceptor: AuthInterceptor): Retrofit =
        Retrofit.Builder()
            .baseUrl(BuildConfig.API_BASE_URL)
            .client(OkHttpClient.Builder().addInterceptor(authInterceptor).build())
            .addConverterFactory(Json.asConverterFactory("application/json".toMediaType()))
            .build()
}

// WalletsRepository.kt
@Singleton
class WalletsRepository @Inject constructor(
    private val api: WalletsApi,
    private val walletsDao: WalletsDao,
) { ... }

// WalletsViewModel.kt
@HiltViewModel
class WalletsViewModel @Inject constructor(
    private val repository: WalletsRepository,
) : ViewModel() { ... }
```

---

## 6. Navigation — Navigation Compose (Type-Safe Routes)

**Library:** `androidx.navigation:navigation-compose` (2.8.x+)

Since Navigation Compose 2.8 (stable, 2024), routes are defined via Kotlin serializable data classes —
no more string-based navigation. This eliminates a whole class of runtime errors.

**Why Navigation Compose:**
- Official Jetpack library, maintained by Google.
- Type-safe destinations via `@Serializable` (no string typos, compile-time checked).
- `NavHost` + `composable<Route>()` integrates naturally with Compose.
- `ShellRoute` equivalent via Compose's `Scaffold` + nested `NavHost` inside tab rows/rail.
- Deep links, back stack management, and animated transitions built in.
- `hiltViewModel()` scoped to the navigation back stack entry — exactly one ViewModel per destination.

**Why not alternatives:**
- **Voyager**: Good community library but not official. Navigation Compose is better supported long-term.
- **Decompose**: Excellent for KMP. Overkill for Android-only.
- **Manual back stack**: Too low-level.

```kotlin
// Routes.kt
@Serializable data object LoginRoute
@Serializable data object DashboardRoute
@Serializable data class WalletDetailRoute(val walletId: String)
@Serializable data class TransactionDetailRoute(val transactionId: String, val walletId: String)

// AppNavGraph.kt
@Composable
fun AppNavGraph(navController: NavHostController) {
    NavHost(navController, startDestination = LoginRoute) {
        composable<LoginRoute> { LoginScreen(navController) }
        composable<DashboardRoute> { DashboardScreen(navController) }
        composable<WalletDetailRoute> { entry ->
            val route: WalletDetailRoute = entry.toRoute()
            WalletDetailScreen(walletId = route.walletId, navController = navController)
        }
    }
}

// Navigate type-safe (no strings):
navController.navigate(WalletDetailRoute(walletId = wallet.id))
```

---

## 7. Theming — Material 3 + Dynamic Color

**No external theming library.** Jetpack Compose's Material 3 + Android 12 Dynamic Color is complete.

**Why:**
- Material 3 (Material You) is Google's design system since 2021. All Android 12+ system apps use it.
- Dynamic Color extracts the color scheme from the user's wallpaper on Android 12+, making the app feel native to each device.
- `isSystemInDarkTheme()` detects the current dark/light mode from system setting.
- Semantic color tokens (`colorScheme.primary`, `colorScheme.surface`, `colorScheme.error`) ensure automatic correct color assignment in both modes without any per-component dark mode handling.
- Compose handles dark/light recomposition automatically when the system theme changes.

```kotlin
// Theme.kt
private val LightColorScheme = lightColorScheme(
    primary = Color(0xFF1A6B3A),
    secondary = Color(0xFF4A8F6B),
    // ... only override what differs from M3 defaults
)

private val DarkColorScheme = darkColorScheme(
    primary = Color(0xFF6EDB98),
    secondary = Color(0xFF89CFAA),
)

@Composable
fun KakeiboTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    dynamicColor: Boolean = true,   // Android 12+
    content: @Composable () -> Unit,
) {
    val colorScheme = when {
        dynamicColor && Build.VERSION.SDK_INT >= Build.VERSION_CODES.S -> {
            if (darkTheme) dynamicDarkColorScheme(LocalContext.current)
            else dynamicLightColorScheme(LocalContext.current)
        }
        darkTheme -> DarkColorScheme
        else -> LightColorScheme
    }

    MaterialTheme(colorScheme = colorScheme, typography = KakeiboTypography) {
        content()
    }
}
```

**User preference:** Persist the user's theme choice (Light / Dark / System) in DataStore.
On app launch, read it before `setContent` to avoid the flash-of-wrong-theme.

---

## 8. Charts — Vico

**Library:** `com.patrykandpatrick.vico:compose-m3` (latest stable)

**GitHub:** patrykandpatrick/vico — 3,000+ stars, actively maintained in 2025-2026.

**Why Vico:**
- **Compose-native**: Built from scratch for Jetpack Compose. No `AndroidView` wrapper, no View system.
  This means smooth, hardware-accelerated rendering integrated with Compose's recomposition model.
- **Material 3**: `compose-m3` artifact integrates with `MaterialTheme.colorScheme` automatically.
  Charts adapt to light/dark mode without any extra code.
- **Chart types**: `CartesianChart` with `LineCartesianLayer` (line/area), `ColumnCartesianLayer` (bars),
  `CandlestickCartesianLayer`. Covers all Kakeibo needs.
- **Animated**: Data changes animate smoothly (spring-based, configurable).
- **Interactive**: Touch-to-tooltip built in via `rememberVicoScrollState()` + `rememberVicoZoomState()`.
- **MIT license**: No attribution requirement, no enterprise plan.

**Why not alternatives:**
- **MPAndroidChart**: 38,000 stars but last commit in 2021. Requires `AndroidView` wrapper in Compose —
  a legacy bridge that breaks Compose rendering, animation, and accessibility. Do not use for new apps.
- **Charty (codeandtheory)**: Less maintained. Fewer chart types.
- **Syncfusion**: Commercial license. Attribution required on free tier.
- **AAChartCore**: Less maintained, fewer examples.

**Charts needed in Kakeibo:**

| Screen | Chart | Vico layer |
|--------|-------|-----------|
| Dashboard (income vs expenses 7/30/90d) | Area/Line | `LineCartesianLayer` with `fill(color).opacity(0.3)` below |
| Budget detail (spending vs limit) | Horizontal bar | `ColumnCartesianLayer` |
| Category breakdown | Pie | Compose's `Canvas` + `drawArc` (pie charts are simple enough without a library) |
| Goal progress | Linear | `LinearProgressIndicator` — Material 3 built-in, no library needed |

```kotlin
// Dashboard area chart — mirrors ChartAreaInteractive.vue
@Composable
fun IncomeExpenseChart(entries: List<ChartEntry>) {
    val modelProducer = remember { CartesianChartModelProducer() }

    LaunchedEffect(entries) {
        modelProducer.runTransaction {
            lineSeries {
                series(entries.map { it.income })
                series(entries.map { it.expense })
            }
        }
    }

    CartesianChartHost(
        chart = rememberCartesianChart(
            rememberLineCartesianLayer(
                lineProvider = LineCartesianLayer.LineProvider.series(
                    LineCartesianLayer.rememberLine(
                        fill = remember { LineCartesianLayer.LineFill.single(Fill(Color(0xFF1A6B3A))) },
                        areaFill = remember {
                            LineCartesianLayer.AreaFill.single(
                                Fill(
                                    ShaderProvider.verticalGradient(
                                        arrayOf(Color(0x401A6B3A), Color(0x001A6B3A))
                                    )
                                )
                            )
                        },
                    ),
                    LineCartesianLayer.rememberLine(
                        fill = remember { LineCartesianLayer.LineFill.single(Fill(Color(0xFFE53935))) },
                    ),
                ),
            ),
            startAxis = VerticalAxis.rememberStart(),
            bottomAxis = HorizontalAxis.rememberBottom(),
        ),
        modelProducer = modelProducer,
    )
}
```

---

## 9. Icons — Material Icons Extended

**Library:** `androidx.compose.material:material-icons-extended`

**Why Material Icons Extended:**
- **2,000+ icons** covering every UI need: navigation, finance (wallet, credit card, savings,
  trending up/down), settings, notifications, etc.
- **Official**: Part of the Compose Material 3 package, maintained by Google.
- **Zero overhead**: Icons are vectors — no SVG parsing, no raster images.
- **RTL auto-mirrored**: Directional icons (arrows, back, forward) flip automatically in RTL locales.
- **Filled + Outlined + Rounded + Sharp + Two-tone variants** per icon — use Outlined for inactive
  nav items, Filled for active. Same pattern as Material 3 navigation guidelines.
- **No additional setup**: Add the dependency, use `Icons.Default.*` or `Icons.Outlined.*`.

**Why not alternatives:**
- **Lucide for Android** (via `br.com.devsrsouza.compose-icons`): The compose-icons project includes
  a Lucide port, but it is community-maintained and does not receive updates at the same cadence as
  the official Lucide releases. For a native Android app, matching web icon packs exactly is less
  important than having a well-maintained, comprehensive icon set. Material Icons Extended is the
  correct choice. Use compose-icons Lucide only if design consistency with the web app is a hard
  requirement from day one.
- **HugeIcons**: Not recommended (same reason as in the web project).
- **Custom SVGs via `painterResource`**: Use only for brand-specific icons (app logo, custom
  illustrations) not present in Material Icons Extended.

```kotlin
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.AccountBalanceWallet
import androidx.compose.material.icons.outlined.TrendingUp
import androidx.compose.material.icons.outlined.Savings
import androidx.compose.material.icons.outlined.ReceiptLong
import androidx.compose.material.icons.outlined.Notifications
import androidx.compose.material.icons.outlined.Settings

// Usage:
Icon(imageVector = Icons.Outlined.AccountBalanceWallet, contentDescription = "Wallets")
```

**Note on icon weight:** The `material-icons-extended` artifact is ~17 MB. Enable R8/ProGuard to
strip unused icons. In production builds, only referenced icons survive, reducing APK size.

---

## 10. Adaptive Layout — WindowSizeClass + Material 3 Adaptive APIs

**Libraries:**
- `androidx.compose.material3.adaptive:adaptive` (WindowSizeClass, NavigationSuiteScaffold)
- `androidx.compose.material3.adaptive:adaptive-navigation` (NavigableListDetailPaneScaffold)

These are official Jetpack libraries, stable since 2024 Google I/O, actively maintained.

**Breakpoints:**

| Window Width | Class | Navigation Component | Equivalent |
|-------------|-------|---------------------|-----------|
| < 600dp (phone portrait) | `Compact` | `BottomNavigationBar` | Mobile nav |
| 600–840dp (tablet portrait / phone landscape) | `Medium` | `NavigationRail` | Collapsed sidebar |
| ≥ 840dp (tablet landscape) | `Expanded` | `NavigationDrawer` | Expanded AppSidebar |

**`NavigationSuiteScaffold`** selects the correct navigation component automatically based on
`currentWindowAdaptiveInfo()`. Zero manual breakpoint logic.

```kotlin
@Composable
fun AppShell(navController: NavHostController) {
    NavigationSuiteScaffold(
        navigationSuiteItems = {
            AppDestination.entries.forEach { destination ->
                item(
                    icon = { Icon(destination.icon, contentDescription = null) },
                    label = { Text(stringResource(destination.label)) },
                    selected = currentDestination?.hasRoute(destination.route) == true,
                    onClick = { navController.navigate(destination.route) },
                )
            }
        }
    ) {
        AppNavGraph(navController)
    }
}
```

**Master-detail on tablet** (WalletDetail, TransactionList) via `NavigableListDetailPaneScaffold`:

```kotlin
@Composable
fun WalletsAdaptiveScreen() {
    val navigator = rememberListDetailPaneScaffoldNavigator<String>()

    NavigableListDetailPaneScaffold(
        navigator = navigator,
        listPane = {
            WalletListPane { walletId ->
                navigator.navigateTo(ListDetailPaneScaffoldRole.Detail, walletId)
            }
        },
        detailPane = {
            val walletId = navigator.currentDestination?.contentKey
            if (walletId != null) WalletDetailPane(walletId)
            else EmptyDetailPane()
        },
    )
}
```

On phone: list and detail are separate screens (back stack). On tablet: side-by-side. The scaffold
handles the layout switch automatically. No duplication of widget trees.

---

## 11. HTTP Client — Retrofit + OkHttp

**Libraries:** `com.squareup.retrofit2:retrofit` + `com.squareup.okhttp3:okhttp` + `com.squareup.okhttp3:logging-interceptor`

**Why Retrofit + OkHttp:**
- 73% of active Android developers use Retrofit (2024–2025 surveys). Largest community, most Stack Overflow answers.
- Mature, battle-tested for 10+ years. Every auth pattern (Bearer, refresh interceptor, cookie) has a reference implementation.
- Kotlin coroutine support via suspend functions — no callbacks, no RxJava.
- `OkHttp` interceptors handle auth token refresh on 401 identically to Axios interceptors in the web app.
- Connection pooling, transparent GZIP, response caching built in.

**Why not Ktor Client:**
Ktor Client is the correct choice for Kotlin Multiplatform projects sharing networking code between
Android and iOS. For an Android-only app, Retrofit is more pragmatic: richer tooling, more documentation,
less ceremony for standard REST + JSON.

```kotlin
// WalletsApi.kt
interface WalletsApi {
    @GET("wallets")
    suspend fun getWallets(): List<WalletDto>

    @POST("wallets")
    suspend fun createWallet(@Body request: CreateWalletRequest): WalletDto

    @DELETE("wallets/{id}")
    suspend fun deleteWallet(@Path("id") id: String): Response<Unit>
}

// AuthInterceptor.kt — mirrors Axios interceptor in Pinia's authStore
class AuthInterceptor @Inject constructor(
    private val tokenStore: TokenStore,
) : Interceptor {
    override fun intercept(chain: Interceptor.Chain): Response {
        val request = chain.request().newBuilder()
            .addHeader("Authorization", "Bearer ${tokenStore.accessToken}")
            .build()
        val response = chain.proceed(request)
        if (response.code == 401) {
            // refresh token logic
        }
        return response
    }
}
```

---

## 12. JSON Serialization — kotlinx.serialization

**Library:** `org.jetbrains.kotlinx:kotlinx-serialization-json`

**Why kotlinx.serialization:**
- **Kotlin-first**: Designed for Kotlin. Supports `object`, `sealed class`, `data class` natively.
- **No reflection**: Uses code generation (KSP). Faster startup, ProGuard-friendly.
- **Integrates with Retrofit**: via `JakeWharton/retrofit2-kotlinx-serialization-converter`.
- **Integrates with Navigation Compose type-safe routes**: `@Serializable` annotation is shared.
- **KMP-ready**: If the project ever expands to iOS, the serialization layer needs zero changes.
- **Benchmark**: 2× faster than Gson for serialization (object → JSON). Competitive with Moshi on deserialization.

**Why not Gson:**
- Java reflection-based. Slower startup. Struggles with Kotlin nullability semantics and `data object`.
- Does not support Kotlin `object` singletons as JSON values.

**Why not Moshi:**
- Good library, but slower than kotlinx.serialization for repeated serialization operations (common in lists).
- Limited KMP support. Moshi's KSP processor is less mature than kotlinx's.

```kotlin
// WalletDto.kt
@Serializable
data class WalletDto(
    val id: String,
    val name: String,
    val type: WalletType,
    val currency: String,
    val isArchived: Boolean,
    val createdAt: String,   // ISO-8601 string from API
)

@Serializable
enum class WalletType { Personal, Shared }
```

---

## 13. Local Storage

### 13.1 User Preferences — Jetpack DataStore

**Library:** `androidx.datastore:datastore-preferences`

DataStore is the official replacement for SharedPreferences. Async, coroutine-based, safe on the main thread.

**Stores:** theme mode (Light/Dark/System), language override, onboarding completed, dashboard period (7d/30d/90d), notification preferences, currency display settings.

```kotlin
val themeKey = stringPreferencesKey("theme_mode")

// Write
context.dataStore.edit { prefs -> prefs[themeKey] = "Dark" }

// Read as Flow
val themeFlow: Flow<String> = context.dataStore.data.map { prefs ->
    prefs[themeKey] ?: "System"
}
```

### 13.2 Offline Cache — Room

**Library:** `androidx.room:room-runtime` + `androidx.room:room-ktx`

Room provides an SQLite abstraction with compile-time query validation, Flow-based reactive queries,
and proper migration support.

**Use for:**
- Caching wallet list and balances (show stale data while refreshing)
- Caching recent transactions (last 50 per wallet, offline readable)
- Caching notifications (badge count survives app restart)
- Caching dashboard chart data (last fetched period)

**Do not use Room for:** anything requiring real-time accuracy (transaction recording, budget calculations).
Those always go to the API. Room is a read cache, not the source of truth.

```kotlin
@Dao
interface WalletsDao {
    @Query("SELECT * FROM wallets WHERE isArchived = 0 ORDER BY createdAt DESC")
    fun observeActiveWallets(): Flow<List<WalletEntity>>

    @Upsert
    suspend fun upsertAll(wallets: List<WalletEntity>)

    @Query("DELETE FROM wallets")
    suspend fun clearAll()
}
```

**Repository pattern (cache-then-network):**

```kotlin
fun getWallets(): Flow<List<Wallet>> = flow {
    // 1. Emit from cache immediately
    emitAll(dao.observeActiveWallets().map { it.map(WalletEntity::toDomain) })
}.onStart {
    // 2. Refresh from API in background
    api.getWallets().onSuccess { dto -> dao.upsertAll(dto.map(WalletDto::toEntity)) }
}
```

### 13.3 Secure Storage — EncryptedDataStore

**Library:** `androidx.security:security-crypto` + DataStore with encryption

For JWT tokens and sensitive values, use `EncryptedSharedPreferences` (from `security-crypto`) or
DataStore with an encrypted backing file. The simpler approach: store tokens in
`EncryptedSharedPreferences` (synchronous, fine for tokens read on interceptor threads):

```kotlin
val encryptedPrefs = EncryptedSharedPreferences.create(
    context,
    "secure_prefs",
    MasterKey.Builder(context).setKeyScheme(MasterKey.KeyScheme.AES256_GCM).build(),
    EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
    EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM,
)
```

---

## 14. Images — Coil

**Library:** `io.coil-kt.coil3:coil-compose`

**Why Coil:**
- **Compose-native**: `AsyncImage` composable, `rememberAsyncImagePainter`. No `ImageView` wrapper.
- **Kotlin-first**: Written in Kotlin, coroutine-based. Follows Kotlin idioms.
- **Efficient**: Memory cache + disk cache. Automatic bitmap pooling. Animated GIF/WebP/SVG support.
- **Actively maintained**: Coil 3.x (2024+) targets Compose Multiplatform — rock-solid for Android.
- **Fast**: Outperforms Glide in many Compose scenarios due to no View → Compose bridging overhead.

**Why not Glide:**
Glide is Java-based and View-first. Works with Compose via `GlideImage`, but Coil is the idiomatic choice.

```kotlin
AsyncImage(
    model = user.avatarUrl,
    contentDescription = "User avatar",
    placeholder = painterResource(R.drawable.ic_avatar_placeholder),
    error = painterResource(R.drawable.ic_avatar_placeholder),
    contentScale = ContentScale.Crop,
    modifier = Modifier.size(40.dp).clip(CircleShape),
)
```

---

## 15. Pagination — Paging 3

**Library:** `androidx.paging:paging-compose`

**Why Paging 3:**
- Official Jetpack library. Integrates with Room, Retrofit, and Compose.
- `LazyPagingItems` in Compose: `items(lazyPagingItems)` — handles loading states, errors, and retries.
- `RemoteMediator` pattern: fetches from API, caches in Room, serves from Room — offline pagination.
- Handles transaction list (50/page), activity feed (100/page), and notification list.

```kotlin
// TransactionsPagingSource.kt
class TransactionsPagingSource(
    private val api: TransactionsApi,
    private val walletId: String,
) : PagingSource<Int, TransactionDto>() {
    override suspend fun load(params: LoadParams<Int>): LoadResult<Int, TransactionDto> {
        val page = params.key ?: 1
        return try {
            val response = api.getTransactions(walletId, page, params.loadSize)
            LoadResult.Page(
                data = response.items,
                prevKey = if (page == 1) null else page - 1,
                nextKey = if (response.items.isEmpty()) null else page + 1,
            )
        } catch (e: Exception) {
            LoadResult.Error(e)
        }
    }
}
```

---

## 16. Push Notifications — Firebase Cloud Messaging

**Libraries:** `com.google.firebase:firebase-messaging-ktx` + `androidx.core:core-ktx` (for notification channels)

**Why FCM:**
- The universal delivery mechanism for Android push notifications — no alternative exists.
- The backend already has a push notification infrastructure (`IWebPushService` with VAPID keys).
  Adding an FCM adapter in the API allows the same event handlers to send to both web browsers
  (Web Push Protocol) and Android devices (FCM) — minimal backend change.
- Free tier covers all typical usage.

**Setup:**
1. Register device token via FCM, send to API on login.
2. API stores token per user session.
3. On events (budget exceeded, invitation received), API sends FCM message.
4. `FirebaseMessagingService` receives it, creates `NotificationCompat` with deep link.
5. Deep link opens the relevant screen via Navigation Compose's `handleDeepLink`.

---

## 17. Date Handling — kotlinx-datetime

**Library:** `org.jetbrains.kotlinx:kotlinx-datetime`

**Why kotlinx-datetime:**
- Kotlin-native equivalent of NodaTime (used in the API). Models: `Instant`, `LocalDate`, `LocalDateTime`, `TimeZone`.
- Works identically to NodaTime's `Instant`/`LocalDate` — matching the API's date semantics.
- `Instant.parse("2026-01-15T10:30:00Z")` — zero-friction JSON date parsing.
- KMP-ready (same API on Android and future iOS if needed).
- **Replaces** `java.util.Date` and `java.util.Calendar` (both prohibited — same philosophy as the API's prohibition of `DateTime`/`DateTimeOffset`).

```kotlin
// Format for display using Java interop:
val instant = Instant.parse(transaction.date)
val localDate = instant.toLocalDateTime(TimeZone.currentSystemDefault()).date
val formatted = DateTimeFormatter.ofPattern("MMM d, yyyy")
    .format(localDate.toJavaLocalDate())
```

---

## 18. Currency Formatting

**No library needed.** Use `java.text.NumberFormat` / `java.util.Currency`:

```kotlin
object CurrencyFormatter {
    fun format(amount: Double, currencyCode: String, locale: Locale = Locale.getDefault()): String {
        val format = NumberFormat.getCurrencyInstance(locale)
        format.currency = Currency.getInstance(currencyCode)
        return format.format(amount)
    }
}
// → "$1,234.56" or "€1.234,56" depending on locale
```

---

## 19. Localization — Android String Resources

**No external library.** Native Android localization is used.

- `res/values/strings.xml` — English (default)
- `res/values-es/strings.xml` — Spanish
- `stringResource(R.string.wallets_title)` in Compose — type-safe, IDE-refactorable.

**Why not external i18n libraries:**
Android's resource system is compile-time checked, IDE-navigable, and handles plurals, gender variants,
and RTL automatically. The existing JSON locale files (`locales/en.json`, `locales/es.json`) from the
web app must be manually translated to `strings.xml` format — a one-time migration.

**Runtime locale switching** (when user changes language in-app without changing device language):

```kotlin
// Override locale at application start based on DataStore preference:
override fun attachBaseContext(base: Context) {
    val langCode = runBlocking { base.dataStore.data.first()[LANGUAGE_KEY] ?: "en" }
    super.attachBaseContext(base.wrapWithLocale(Locale(langCode)))
}
```

---

## 20. Forms and Validation

**No external forms library.** Compose + ViewModel handle forms cleanly:

- One `TextFieldUiState(value, error)` per form field in the ViewModel.
- Validation logic lives in the ViewModel — pure Kotlin, testable without Compose.
- Form submission triggers the ViewModel function which validates and calls the repository.

```kotlin
data class CreateTransactionFormState(
    val type: TransactionType = TransactionType.Expense,
    val amount: String = "",
    val amountError: String? = null,
    val description: String = "",
    val descriptionError: String? = null,
    val date: LocalDate = Clock.System.todayIn(TimeZone.currentSystemDefault()),
    val categoryId: String? = null,
    val categoryError: String? = null,
    val isSubmitting: Boolean = false,
    val submitError: String? = null,
)

// Validation in ViewModel:
private fun validate(state: CreateTransactionFormState): CreateTransactionFormState {
    val amount = state.amount.toDoubleOrNull()
    return state.copy(
        amountError = when {
            amount == null -> "Invalid amount"
            amount < 0.01 -> "Minimum amount is 0.01"
            amount > 999_999_999.99 -> "Maximum amount exceeded"
            else -> null
        },
        descriptionError = if (state.description.length > 500) "Max 500 characters" else null,
        categoryError = if (state.categoryId == null) "Category required" else null,
    )
}
```

---

## 21. Miscellaneous Utilities

| Library | Purpose | Justification |
|---------|---------|---------------|
| `androidx.compose.material3:material3` | Shimmer / skeleton loading | `Placeholder` composable from Material 3 Experimental, or use `compose-shimmer` (`com.valentinilk.shimmer:shimmer`) — 1,500 stars, Compose-native, actively maintained |
| `com.jakewharton.timber:timber` | Logging | Structured logging, log levels, log tree swapping in tests. The standard for Android logging. |
| `androidx.core:core-splashscreen` | Splash screen | Material splash with branded icon. Handles 12+ splash API and older devices. |
| `androidx.biometric:biometric` | Biometric auth (optional) | Fingerprint / face unlock for app lock feature. Official Jetpack. |
| `com.google.accompanist:accompanist-permissions` | Runtime permissions | Declarative permission request in Compose (camera, notifications). Actively maintained. |
| `com.valentinilk.shimmer:shimmer` | Shimmer loading effect | Compose-native, mirrors Vue's `Skeleton` component behavior during data loading. |
| `timeago4j` | Relative time strings | "2 hours ago" for notifications and activity feed. Simple, no dependencies. Alternative: manual `Duration.between` formatting. |

**SwipeToDismiss (delete/archive):** Use `SwipeToDismissBox` from `androidx.compose.material3`. Built-in Material 3, no external library needed.

**Share / Export:** Use Android's `Intent.ACTION_SEND` with `FileProvider` for sharing exported CSV/JSON files. No external library needed.

**Connectivity:** Use `ConnectivityManager.NetworkCallback` registered in a `Repository` or `ViewModel` to detect offline. Expose as a `StateFlow<Boolean>` from a `ConnectivityObserver` singleton.

---

## 22. Testing

**Strategy: unit-heavy, integration where needed, E2E for critical flows.**

| Layer | Tool | What it tests |
|-------|------|--------------|
| ViewModel / domain logic | JUnit 5 + `kotlinx-coroutines-test` | UiState transitions, validation, repository error handling |
| Composables | `androidx.compose.ui:ui-test-junit4` | Screen rendering, user interactions, accessibility |
| Repository (API) | `okhttp3:mockwebserver` | HTTP response parsing, error mapping, interceptor logic |
| Room DAO | In-memory Room (`allowMainThreadQueries()`) | SQL query correctness |
| End-to-end | `io.github.kakaocup:kakao` + `androidx.test.espresso` | Login → wallet → transaction happy path |
| Mocking | `io.mockk:mockk` | Kotlin-native mocking. No code generation. Replaces Mockito for Kotlin. |

```kotlin
// ViewModel unit test example:
@OptIn(ExperimentalCoroutinesApi::class)
class AuthViewModelTest {
    @get:Rule val coroutineRule = MainCoroutineRule()

    private val authRepository = mockk<AuthRepository>()
    private val viewModel by lazy { AuthViewModel(authRepository) }

    @Test
    fun `login success emits Success state`() = runTest {
        coEvery { authRepository.login(any(), any()) } returns Result.success(fakeUser)

        viewModel.login("user@example.com", "password")
        advanceUntilIdle()

        assertThat(viewModel.uiState.value).isInstanceOf(LoginUiState.Success::class.java)
    }
}
```

---

## 23. Project Configuration

### 23.1 Build System

**Gradle (Kotlin DSL)** — the standard for Android. Use `gradle/libs.versions.toml` (version catalog)
to centralize all dependency versions and avoid version drift across modules.

```toml
# gradle/libs.versions.toml
[versions]
kotlin = "2.1.0"
compose-bom = "2025.01.00"
hilt = "2.52"
retrofit = "2.11.0"
room = "2.6.1"
vico = "2.0.1"

[libraries]
compose-bom = { module = "androidx.compose:compose-bom", version.ref = "compose-bom" }
compose-ui = { module = "androidx.compose.ui:ui" }
compose-material3 = { module = "androidx.compose.material3:material3" }
hilt-android = { module = "com.google.dagger:hilt-android", version.ref = "hilt" }
vico-compose-m3 = { module = "com.patrykandpatrick.vico:compose-m3", version.ref = "vico" }
```

### 23.2 Folder Structure (Module Strategy)

For a single-developer app, a **single `:app` module** is correct. Multi-module (`:feature:wallets`,
`:feature:transactions`) adds build time and complexity without benefit at this scale. The feature
folder structure inside `:app` provides the same logical separation.

Migrate to multi-module only when:
- Build times exceed 2 minutes and feature isolation is needed.
- Multiple developers work on separate features and need compilation isolation.

### 23.3 ProGuard / R8

Enable R8 (enabled by default in release builds). Add rules for:
- kotlinx.serialization: `@Keep` on serializable classes, or use the provided consumer rules.
- Retrofit: keep API interfaces.
- Vico: keep chart model classes.
- Material Icons Extended: R8 strips unused icons automatically.

---

## 24. Complete Dependency List (`build.gradle.kts`)

```kotlin
dependencies {
    // Compose BOM (manages all Compose library versions consistently)
    val composeBom = platform("androidx.compose:compose-bom:2025.01.00")
    implementation(composeBom)
    implementation("androidx.compose.ui:ui")
    implementation("androidx.compose.ui:ui-tooling-preview")
    implementation("androidx.compose.material3:material3")
    implementation("androidx.compose.material:material-icons-extended")

    // Compose + Activity
    implementation("androidx.activity:activity-compose:1.9.3")
    implementation("androidx.lifecycle:lifecycle-viewmodel-compose:2.8.7")
    implementation("androidx.lifecycle:lifecycle-runtime-compose:2.8.7")

    // Navigation
    implementation("androidx.navigation:navigation-compose:2.8.5")

    // Material 3 Adaptive (adaptive scaffold, WindowSizeClass)
    implementation("androidx.compose.material3.adaptive:adaptive:1.1.0")
    implementation("androidx.compose.material3.adaptive:adaptive-navigation:1.1.0")
    implementation("androidx.compose.material3.adaptive:adaptive-layout:1.1.0")

    // Dependency Injection
    implementation("com.google.dagger:hilt-android:2.52")
    kapt("com.google.dagger:hilt-android-compiler:2.52")
    implementation("androidx.hilt:hilt-navigation-compose:1.2.0")

    // Networking
    implementation("com.squareup.retrofit2:retrofit:2.11.0")
    implementation("com.squareup.okhttp3:okhttp:4.12.0")
    implementation("com.squareup.okhttp3:logging-interceptor:4.12.0")
    implementation("com.jakewharton.retrofit:retrofit2-kotlinx-serialization-converter:1.0.0")

    // Serialization
    implementation("org.jetbrains.kotlinx:kotlinx-serialization-json:1.7.3")

    // Charts
    implementation("com.patrykandpatrick.vico:compose-m3:2.0.1")

    // Images
    implementation("io.coil-kt.coil3:coil-compose:3.1.0")
    implementation("io.coil-kt.coil3:coil-network-okhttp:3.1.0")

    // Local Storage
    implementation("androidx.room:room-runtime:2.6.1")
    implementation("androidx.room:room-ktx:2.6.1")
    kapt("androidx.room:room-compiler:2.6.1")
    implementation("androidx.datastore:datastore-preferences:1.1.1")

    // Security (encrypted prefs for tokens)
    implementation("androidx.security:security-crypto:1.0.0")

    // Pagination
    implementation("androidx.paging:paging-runtime-ktx:3.3.5")
    implementation("androidx.paging:paging-compose:3.3.5")

    // Push Notifications
    implementation("com.google.firebase:firebase-messaging-ktx:24.1.0")
    implementation(platform("com.google.firebase:firebase-bom:33.7.0"))

    // Date
    implementation("org.jetbrains.kotlinx:kotlinx-datetime:0.6.1")

    // Splash screen
    implementation("androidx.core:core-splashscreen:1.0.1")

    // Permissions in Compose
    implementation("com.google.accompanist:accompanist-permissions:0.37.0")

    // Shimmer loading
    implementation("com.valentinilk.shimmer:compose-shimmer:1.3.1")

    // Logging
    implementation("com.jakewharton.timber:timber:5.0.1")

    // Testing
    testImplementation("junit:junit:4.13.2")
    testImplementation("org.jetbrains.kotlinx:kotlinx-coroutines-test:1.9.0")
    testImplementation("io.mockk:mockk:1.13.13")
    testImplementation("com.google.truth:truth:1.4.4")
    androidTestImplementation(composeBom)
    androidTestImplementation("androidx.compose.ui:ui-test-junit4")
    androidTestImplementation("com.squareup.okhttp3:mockwebserver:4.12.0")
    debugImplementation("androidx.compose.ui:ui-tooling")
    debugImplementation("androidx.compose.ui:ui-test-manifest")
}
```

---

## 25. Technologies Explicitly Excluded

| Technology | Reason |
|-----------|--------|
| **MPAndroidChart** | Unmaintained since 2021. Requires `AndroidView` wrapper in Compose — defeats Compose rendering model. |
| **Syncfusion Charts** | Enterprise license. Attribution required on free tier. |
| **Koin** | Runtime DI misses dependency errors until first use. Hilt catches them at compile time. |
| **Glide** | Java-based, View-first. Coil is Kotlin-native and Compose-idiomatic. |
| **GetX (Dart port)** | Not applicable — Android only. No equivalent pattern recommended. |
| **Gson** | Java reflection-based, does not understand Kotlin nullability or `object`. |
| **Moshi** | Slightly slower for lists. Limited KMP support. kotlinx.serialization is strictly better for this project. |
| **Ktor Client** | Correct choice for KMP projects. Android-only project benefits more from Retrofit's documentation. |
| **SharedPreferences** | Deprecated by Google. DataStore is the official replacement. |
| **java.util.Date / Calendar** | Use kotlinx-datetime. Same philosophy as the API's prohibition of DateTime. |
| **RxJava** | Legacy reactive. Coroutines + Flow cover every use case with less ceremony. |
| **Auto Route / Voyager** | Community navigation libs. Navigation Compose (official, Google-maintained) is correct. |
| **flutter_adaptive_scaffold** | Flutter package. Not applicable here. |
| **Multi-module from day one** | Premature optimization. Single `:app` module until build times justify the split. |

---

## 26. Summary Table

| Concern | Decision | Library |
|---------|----------|---------|
| Language + UI | Kotlin + Jetpack Compose | Built-in (Kotlin 2.x, Compose BOM) |
| Architecture | MVVM + UDF + Feature Vertical Slices | — |
| State management | ViewModel + StateFlow + UiState | `lifecycle-viewmodel-compose` |
| Dependency injection | Hilt | `hilt-android`, `hilt-navigation-compose` |
| Navigation | Navigation Compose (type-safe) | `navigation-compose` |
| Theming | Material 3 + Dynamic Color | `material3` (Compose BOM) |
| Charts | Vico | `vico:compose-m3` |
| Icons | Material Icons Extended | `material-icons-extended` |
| Adaptive layout | WindowSizeClass + NavigationSuiteScaffold | `adaptive`, `adaptive-navigation` |
| HTTP client | Retrofit + OkHttp | `retrofit`, `okhttp` |
| JSON serialization | kotlinx.serialization | `kotlinx-serialization-json` |
| Images | Coil 3 | `coil-compose` |
| Pagination | Paging 3 | `paging-compose` |
| Local cache | Room | `room-runtime`, `room-ktx` |
| User preferences | DataStore | `datastore-preferences` |
| Secure token storage | EncryptedSharedPreferences | `security-crypto` |
| Push notifications | Firebase Cloud Messaging | `firebase-messaging-ktx` |
| Date/time | kotlinx-datetime | `kotlinx-datetime` |
| Currency/date format | Java NumberFormat / DateTimeFormatter | Built-in |
| Localization | Android string resources | Built-in |
| Forms & validation | ViewModel + UiState | — |
| Loading states | Shimmer | `compose-shimmer` |
| Logging | Timber | `timber` |
| Splash screen | Core SplashScreen API | `core-splashscreen` |
| Unit testing | JUnit 4 + MockK + coroutines-test | `mockk`, `kotlinx-coroutines-test` |
| UI testing | Compose UI Test | `ui-test-junit4` |
| Mocking | MockK | `mockk` |

---

*Study date: 2026-03-08*
*Scope: full Kakeibo feature set — native Android (Android 8.0+ / API 26+), Kotlin 2.x, Jetpack Compose*
*Quality reference: polish level comparable to Wilo.App (modern finance/wellness app UX)*
