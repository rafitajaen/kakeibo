# Kakeibo Android

Native Android application replicating the full Kakeibo feature set.
Built with Kotlin 2.x + Jetpack Compose. Targets Android 8.0+ (API 26).

> **UI Reference:** See `studies/onemoney-ui-study.md` for UX patterns to implement
> (persistent net balance header, date-grouped transactions, coloured entity icons, etc.)

---

## Tech Stack

| Concern | Decision | Library / Source |
|---------|----------|--------------------|
| Language + UI | Kotlin 2.x + Jetpack Compose | Built-in (Compose BOM 2025.01.00) |
| Architecture | MVVM + UDF + Feature Vertical Slices | — |
| State management | ViewModel + StateFlow + sealed UiState | `lifecycle-viewmodel-compose` |
| Dependency injection | Hilt | `hilt-android`, `hilt-navigation-compose` |
| Navigation | Navigation Compose 2.8+ (type-safe `@Serializable` routes) | `navigation-compose` |
| Theming | Material 3 + Dynamic Color (Android 12+) | `material3` (Compose BOM) |
| Charts | Vico | `vico:compose-m3` |
| Icons | Material Icons Extended | `material-icons-extended` |
| Adaptive layout | NavigationSuiteScaffold + NavigableListDetailPaneScaffold | `adaptive`, `adaptive-navigation` |
| HTTP client | Retrofit 2 + OkHttp 4 | `retrofit`, `okhttp` |
| JSON | kotlinx.serialization | `kotlinx-serialization-json` |
| Images | Coil 3 | `coil-compose`, `coil-network-okhttp` |
| Pagination | Paging 3 | `paging-compose` |
| Offline cache | Room | `room-runtime`, `room-ktx` |
| User preferences | DataStore Preferences | `datastore-preferences` |
| Token storage | EncryptedSharedPreferences | `security-crypto` |
| Push notifications | Firebase Cloud Messaging | `firebase-messaging-ktx` |
| Date / time | kotlinx-datetime | `kotlinx-datetime` |
| Currency format | `java.text.NumberFormat` | Built-in JVM |
| Localization | Android string resources (`res/values*/strings.xml`) | Built-in |
| Forms & validation | ViewModel + UiState (no form library) | — |
| Shimmer / loading | compose-shimmer | `compose-shimmer` |
| Logging | Timber | `timber` |
| Splash screen | Core SplashScreen API | `core-splashscreen` |
| Unit tests | JUnit 4 + MockK + coroutines-test | `mockk`, `kotlinx-coroutines-test` |
| UI tests | Compose UI Test | `ui-test-junit4` |
| API mock tests | MockWebServer | `mockwebserver` |

---

## Prohibited Technologies

| Technology | Reason |
|------------|--------|
| **MPAndroidChart** | Unmaintained (2021). Requires `AndroidView` wrapper — defeats Compose rendering model. |
| **Syncfusion Charts** | Enterprise licence. Attribution required on free tier. |
| **Koin** | Runtime DI — dependency errors only surface at first use. Hilt catches them at compile time. |
| **Glide** | Java-based, View-first. Coil is Kotlin-native and Compose-idiomatic. |
| **Gson** | Java reflection-based. Does not understand Kotlin nullability or `object`. |
| **Moshi** | Slower for list serialisation. Limited KMP support. kotlinx.serialization is strictly better. |
| **Ktor Client** | Correct for KMP. Android-only benefits more from Retrofit's documentation and tooling. |
| **SharedPreferences** | Deprecated by Google. DataStore is the official replacement for non-sensitive prefs. |
| **java.util.Date / Calendar** | Use `kotlinx-datetime`. Same philosophy as the API's prohibition of `DateTime`. |
| **RxJava** | Legacy reactive. Coroutines + Flow cover every use case with less ceremony. |
| **Voyager / Decompose** | Community navigation libs. Navigation Compose (Google-maintained) is the correct choice. |
| **Multi-module from day one** | Premature optimisation. Single `:app` module until build times justify the split. |
| **@hugeicons** | Use `material-icons-extended` (offline, zero overhead, RTL, officially maintained). |
| **HugeIcons / Lucide Android** | Same as above. |
| **XML View layouts** | Jetpack Compose is the only UI layer. No `layout/*.xml`, no `RecyclerView`, no `ViewHolder`. |
| **MediatR / MVVM Light** | Unnecessary for single-developer Android projects. ViewModel + StateFlow is sufficient. |

---

## Architecture

### Pattern: MVVM + Unidirectional Data Flow (UDF) + Feature Vertical Slices

Each feature is self-contained under `features/{domain}/`:

```
app/src/main/java/com/kakeibo/
  core/
    api/                  # OkHttp interceptors, ApiResult<T>
    auth/                 # TokenStore (EncryptedSharedPreferences)
    db/                   # KakeiboDatabase (Room)
    navigation/           # Type-safe routes, NavHost, AppShell
    theme/                # KakeiboTheme, Typography
    di/                   # Hilt NetworkModule, DatabaseModule
    push/                 # FCM service
  features/
    auth/
      data/               # AuthRepository, AuthApi (Retrofit)
      domain/             # LoginRequest, RegisterRequest, User
      presentation/       # LoginScreen, RegisterScreen, AuthViewModel, AuthUiState
    dashboard/
    wallets/
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

### Layering per Feature

1. **`domain/`** — Pure Kotlin data classes. No Android/Compose imports. `@Serializable`.
2. **`data/`** — Repository + Retrofit `@interface`. Returns `Result<T>` or `Flow<T>`.
3. **`presentation/`** — `ViewModel` + `UiState` (sealed interface) + Composable screens.

### UiState Pattern

```kotlin
// Every screen has a sealed UiState
sealed interface WalletListUiState {
    data object Loading : WalletListUiState
    data class Success(val wallets: List<Wallet>) : WalletListUiState
    data class Error(val message: String) : WalletListUiState
}

// ViewModel exposes StateFlow
@HiltViewModel
class WalletListViewModel @Inject constructor(
    private val repository: WalletsRepository,
) : ViewModel() {
    private val _uiState = MutableStateFlow<WalletListUiState>(WalletListUiState.Loading)
    val uiState: StateFlow<WalletListUiState> = _uiState.asStateFlow()
}

// Screen collects lifecycle-aware
@Composable
fun WalletListScreen(viewModel: WalletListViewModel = hiltViewModel()) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    // ...
}
```

### One-shot Events (navigation, snackbars)

Use `Channel<Event>` exposed as `receiveAsFlow()` — not `SharedFlow`. A channel delivers
exactly once and is not replayed on recomposition. Prevents double-navigation on rotation.

---

## Naming Conventions

| Artefact | Suffix | Example |
|----------|--------|---------|
| Composable screen | `Screen` | `WalletDetailScreen.kt` |
| ViewModel | `ViewModel` | `WalletDetailViewModel.kt` |
| UI state | `UiState` | `WalletDetailUiState.kt` |
| Repository | `Repository` | `WalletsRepository.kt` |
| Retrofit interface | `Api` | `WalletsApi.kt` |
| Room DAO | `Dao` | `WalletsDao.kt` |
| Room entity | `Entity` | `WalletEntity.kt` |
| Network DTO | `Dto` | `WalletDto.kt` |
| Navigation route | `Route` | `WalletDetailRoute` |
| Hilt module | `Module` | `NetworkModule.kt` |
| Event bus event | `Event` | `TokenExpiredEvent` |

---

## Package Conventions

```
com.kakeibo.features.{domain}.data        # Repository, Api, Dto, Entity, Dao
com.kakeibo.features.{domain}.domain      # Pure Kotlin data classes (no Android imports)
com.kakeibo.features.{domain}.presentation  # Screen, ViewModel, UiState
```

---

## Date and Time

**Use `kotlinx-datetime`** — the Kotlin-native equivalent of NodaTime (used in the API).

```kotlin
// Parse ISO-8601 string from API
val instant = Instant.parse(transaction.date)
val localDate = instant.toLocalDateTime(TimeZone.currentSystemDefault()).date

// Format for display
val formatted = DateTimeFormatter.ofPattern("MMM d, yyyy")
    .format(localDate.toJavaLocalDate())

// Current date
val today = Clock.System.todayIn(TimeZone.currentSystemDefault())
```

**Prohibited:** `java.util.Date`, `java.util.Calendar`, `java.time.LocalDate` in domain
layer (use kotlinx-datetime types). Java interop only at the formatting layer.

---

## Currency Formatting

```kotlin
object CurrencyFormatter {
    fun format(amount: Double, currencyCode: String, locale: Locale = Locale.getDefault()): String {
        val format = NumberFormat.getCurrencyInstance(locale)
        format.currency = Currency.getInstance(currencyCode)
        return format.format(amount)
    }
}
```

---

## Colour Conventions (from OneMoney UI study)

| Concept | Colour token |
|---------|--------------|
| Income / positive balance | `MaterialTheme.colorScheme.tertiary` (green) |
| Expense / negative balance | `MaterialTheme.colorScheme.error` (red/pink) |
| Transfer | `MaterialTheme.colorScheme.secondary` |
| Interactive / accent | `MaterialTheme.colorScheme.primary` |
| Category icons | Per-category solid fill circle — user-defined or system default |

---

## Build Commands

```bash
cd src/Kakeibo.Android

./gradlew assembleDebug        # Build debug APK
./gradlew assembleRelease      # Build release APK (requires signing config)
./gradlew test                 # Unit tests (JVM, no device)
./gradlew connectedCheck       # Instrumented tests (requires running emulator/device)
./gradlew lint                 # Lint
./gradlew dependencies         # Show dependency tree
```

Debug APK is output to `app/build/outputs/apk/debug/app-debug.apk`.

---

## Environment Setup

1. Copy `local.properties.example` → `local.properties`
2. Set `sdk.dir=/path/to/your/Android/Sdk`
3. Copy `app/google-services.json.example` → `app/google-services.json` and fill in real FCM values
4. Run `./gradlew assembleDebug` to verify

See `.claude/guides/android/00-setup.md` for the full step-by-step guide.

---

## API Connection

| Environment | URL |
|-------------|-----|
| Emulator (debug) | `http://10.0.2.2:5000/api/` (`10.0.2.2` = host machine `localhost`) |
| Physical device (debug) | `http://YOUR_LAN_IP:5000/api/` |
| Production | `https://api.kakeibo.app/api/` |

Configured in `app/build.gradle.kts` via `buildConfigField("String", "API_BASE_URL", ...)`.
