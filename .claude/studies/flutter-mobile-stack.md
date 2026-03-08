# Flutter Mobile Stack Study — Kakeibo App

> Comprehensive technology selection for a Flutter mobile application replicating
> the full Kakeibo.App feature set. Covers state management, navigation, theming,
> charts, icons, localization, networking, forms, storage, and architecture.
> Every decision is justified and a single recommended option is given.

---

## 1. Scope

The Flutter app must replicate all 37 screens of Kakeibo.App:

- **Auth**: login, register, email verification, forgot password, reset password
- **Wallets**: list, detail, create, edit, shared wallet management, invite members
- **Transactions**: list per wallet, record, edit; with file attachments
- **Categories**: list (system + custom), create, edit
- **Budgets**: list, create, edit; with progress visualization
- **Goals**: list, create, edit; with milestone tracking
- **Recurring**: list, create, edit; with 30/90-day forecast
- **Collaboration**: friends, friend requests, shared wallet members, settlements
- **Notifications**: notification list, mark-as-read, push subscription, preferences
- **Activity Feed**: paginated audit log with filters
- **Dashboard**: balance overview, area chart (7d/30d/90d), section metric cards, budget summary, goal summary, recent transactions, quick actions
- **Settings**: profile, password, sessions, display preferences, import/export
- **Admin**: user management, platform policy (super-admin only)
- **Onboarding**: 4-step wizard with optional seed data

**Constraints:**
- Simple to understand and extend
- Good scalability
- Excellent theming (light/dark)
- Modern, beautiful charts
- Good icon library
- Works well on both phone and tablet
- Works on Android and iOS

---

## 2. Language & Flutter Version

**Flutter 3.27+ (stable)** with **Dart 3.6+**.

- Null safety is mandatory (Dart 2.12+)
- Records, patterns, sealed classes available (Dart 3.x) — use for discriminated unions
- Material 3 is the default design system since Flutter 3.16+

No reason to consider any other framework (React Native, KMM) — Flutter is the best choice for
cross-platform Android + iOS with a single codebase, native performance, and the best widget
ecosystem.

---

## 3. Architecture

### 3.1 Overall Pattern: Feature-based Vertical Slices

Mirror the API's folder structure. Each feature is self-contained:

```
lib/
  core/                        # Cross-cutting concerns
    api/                       # HTTP client, interceptors
    auth/                      # Auth state, token management
    routing/                   # GoRouter configuration
    theme/                     # Theme definitions
    l10n/                      # Localization
    utils/                     # Currency, date formatting
  features/
    auth/
      data/                    # AuthRepository, AuthApi
      domain/                  # User model (freezed)
      presentation/
        screens/               # LoginScreen, RegisterScreen, etc.
        providers/             # AuthNotifier (Riverpod)
        widgets/               # LoginForm, RegisterForm, etc.
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
    dashboard/
    admin/
    onboarding/
```

### 3.2 Layering per Feature

1. **domain/**: Pure Dart models (freezed), no Flutter imports
2. **data/**: Repository classes that call the API. Returns domain models.
3. **presentation/providers/**: Riverpod notifiers. Call repositories. Expose async state.
4. **presentation/screens/**: ConsumerWidget screens. Watch providers.
5. **presentation/widgets/**: Reusable widgets within the feature.

This is "clean architecture lite" — simple enough for a single developer, scalable enough for a
full app.

---

## 4. State Management — Riverpod

**Packages:** `flutter_riverpod` + `riverpod_annotation` + `riverpod_generator`

**Why Riverpod:**
- Type-safe by design — providers are typed, no dynamic lookups
- Compile-time safe with `riverpod_generator` (catches errors at build, not runtime)
- `AsyncNotifierProvider` maps directly to Pinia's async actions pattern
- `FutureProvider` for simple read-only data fetching
- Auto-dispose: providers cleaned up when not in use (perfect for paginated screens)
- No `BuildContext` required for reading state outside the widget tree
- Official, actively maintained, recommended by the Flutter community

**Why not alternatives:**
- **BLoC**: 3× more boilerplate. Events + states + bloc class for each feature. Overkill for a
  single-developer app.
- **Provider**: Superseded by Riverpod. Not type-safe. Lacks proper scoping.
- **GetX**: All-in-one framework (routing + state + DI). Opinionated, hard to test, not
  composable.
- **MobX**: Observable annotations everywhere. Less idiomatic in Flutter.

**Pattern:**

```dart
// domain/models/wallet.dart
@freezed
class Wallet with _$Wallet {
  const factory Wallet({
    required String id,
    required String name,
    required String type,
    required String currency,
    required double balance,
    required bool isArchived,
    // ...
  }) = _Wallet;

  factory Wallet.fromJson(Map<String, dynamic> json) => _$WalletFromJson(json);
}

// data/wallets_repository.dart
@riverpod
WalletsRepository walletsRepository(WalletsRepositoryRef ref) {
  return WalletsRepository(ref.watch(apiClientProvider));
}

// presentation/providers/wallets_provider.dart
@riverpod
class WalletsNotifier extends _$WalletsNotifier {
  @override
  Future<List<Wallet>> build() =>
      ref.watch(walletsRepositoryProvider).getWallets();

  Future<void> createWallet(CreateWalletRequest request) async {
    final newWallet =
        await ref.read(walletsRepositoryProvider).createWallet(request);
    state = AsyncData([...state.requireValue, newWallet]);
  }
}
```

---

## 5. Navigation — GoRouter

**Packages:** `go_router` + `go_router_builder` (type-safe route code generation)

**Why GoRouter:**
- Official Flutter team package
- Declarative, URL-based — deep links work out of the box
- `ShellRoute` + `StatefulShellRoute` for persistent bottom navigation and adaptive layouts
- Route guards via `redirect` — identical to Vue Router's `requiresAuth` meta
- Named routes + path parameters (`/wallets/:id`)
- Nested navigation stacks per tab
- Android back button and iOS swipe-to-go-back work correctly

**Why not alternatives:**
- **Auto Route**: Good but GoRouter is official and has more documentation.
- **Beamer**: Less active development.
- **Navigator 2.0 directly**: Too low-level for a full app.

**Route guard example (mirrors Vue Router meta):**

```dart
GoRouter(
  redirect: (context, state) {
    final isLoggedIn = ref.read(authProvider).isLoggedIn;
    final isAuthRoute = state.matchedLocation.startsWith('/login');
    if (!isLoggedIn && !isAuthRoute) return '/login';
    if (isLoggedIn && isAuthRoute) return '/';
    return null;
  },
  routes: [
    ShellRoute(
      builder: (context, state, child) => AppShell(child: child),
      routes: [
        GoRoute(path: '/', builder: (c, s) => const DashboardScreen()),
        GoRoute(
          path: '/wallets',
          builder: (c, s) => const WalletsScreen(),
          routes: [
            GoRoute(
              path: ':id',
              builder: (c, s) =>
                  WalletDetailScreen(id: s.pathParameters['id']!),
            ),
          ],
        ),
      ],
    ),
    GoRoute(path: '/login', builder: (c, s) => const LoginScreen()),
  ],
)
```

---

## 6. Theming — Material 3 + flex_color_scheme

**Package:** `flex_color_scheme`

Flutter 3.16+ defaults to Material 3. `flex_color_scheme` provides:
- Beautiful, pre-built color schemes (light + dark)
- Correct M3 surface tones and elevation tints across all 30+ color roles
- One-liner `ThemeData` setup
- Adaptive theming (phone/tablet surface adjustments)
- Seed color generation matching Material You on Android

**Why flex_color_scheme over manual ThemeData:**
Generating a correct M3 color scheme manually requires deep knowledge of color roles (primary,
secondary, tertiary, error, surface, surfaceVariant, etc.). `flex_color_scheme` gets all of these
right from a single seed color. The resulting UI looks professional without design expertise.

```dart
MaterialApp.router(
  theme: FlexThemeData.light(
    scheme: FlexScheme.blueM3,
    useMaterial3: true,
  ),
  darkTheme: FlexThemeData.dark(
    scheme: FlexScheme.blueM3,
    useMaterial3: true,
  ),
  themeMode: ThemeMode.system, // overridden by user preference
)
```

---

## 7. Charts — fl_chart

**Package:** `fl_chart`

**Why fl_chart:**
- Most popular Flutter charting library (6k+ stars, actively maintained)
- Supports `LineChart`, `BarChart`, `PieChart`, `RadarChart`, `ScatterChart`
- Smooth animations on data change
- Interactive touch callbacks (tap, longpress, tooltip)
- Gradient fills under lines — identical to the Unovis area chart in the web app
- MIT licensed — no attribution or enterprise concerns

**Charts needed in Kakeibo:**
- **Dashboard area chart (7d/30d/90d toggle):** `LineChart` with `belowBarData` gradient fill
- **Budget progress bars:** `LinearProgressIndicator` with M3 styling (no chart package needed)
- **Goal progress bars:** Same as above
- **Optional pie chart (category breakdown):** `PieChart`

**Why not alternatives:**
- **syncfusion_flutter_charts**: Enterprise license. Free tier requires attribution. Overkill.
- **graphic**: Less maintained, fewer examples.
- **community_charts_flutter**: Unmaintained since 2021. Avoid.
- **Unovis**: Web only (SVG/Canvas). Not available for Flutter.

**Area chart example (mirrors ChartAreaInteractive.vue):**

```dart
LineChart(
  LineChartData(
    lineBarsData: [
      LineChartBarData(
        spots: incomeSpots,
        gradient: LinearGradient(colors: [Colors.green, Colors.greenAccent]),
        belowBarData: BarAreaData(
          show: true,
          gradient: LinearGradient(
            colors: [Colors.green.withOpacity(0.3), Colors.transparent],
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
          ),
        ),
      ),
      LineChartBarData(spots: expenseSpots, color: Colors.red),
    ],
    titlesData: FlTitlesData(/* ... */),
    gridData: FlGridData(show: true),
    borderData: FlBorderData(show: false),
    touchData: LineTouchData(touchTooltipData: LineTouchTooltipData(/* ... */)),
  ),
)
```

---

## 8. Icons — lucide_icons

**Package:** `lucide_icons`

**Why lucide_icons:**
- Direct Flutter equivalent of `lucide-vue-next` used in the web app
- Same icon set, same names — keeps design language consistent across web and mobile
- 1,300+ icons rendered as Flutter `IconData` (no SVG runtime needed)
- MIT licensed
- Synced with Lucide releases

**Why not alternatives:**
- **Material Icons (built-in)**: Different visual style. Acceptable for system chrome (back, close,
  menu), but Lucide should be used for all feature icons to maintain consistency.
- **Font Awesome Flutter**: Different icon set — creates inconsistency with the web.
- **flutter_svg**: Use alongside `lucide_icons` for the app logo or any custom brand icons not
  in Lucide.
- **HugeIcons**: No advantage over Lucide for this project.

```dart
import 'package:lucide_icons/lucide_icons.dart';

Icon(LucideIcons.wallet)
Icon(LucideIcons.arrowUpRight)
Icon(LucideIcons.settings)
Icon(LucideIcons.piggyBank)
```

---

## 9. Responsive Layout — flutter_adaptive_scaffold + flutter_screenutil

### 9.1 flutter_adaptive_scaffold

**Package:** `flutter_adaptive_scaffold` (official Flutter team)

The app must work on phone (360–414dp) and tablet (768–1024dp). `AdaptiveScaffold` automatically
switches navigation component based on screen width:

| Screen Width | Navigation | Content |
|-------------|-----------|---------|
| < 600dp (phone) | `BottomNavigationBar` | Full width |
| 600–840dp (tablet portrait) | `NavigationRail` (icons + labels) | Remaining width |
| ≥ 840dp (tablet landscape) | `NavigationDrawer` (full sidebar) | Remaining width |

The ≥ 840dp layout is identical to the web app's `AppSidebar` collapsible sidebar.

**Why not manual `LayoutBuilder`:**
Manual breakpoint handling requires duplicating widget trees. `flutter_adaptive_scaffold` handles
transitions, animations, and accessibility correctly out of the box.

**Master-detail for list screens (WalletDetail, TransactionList):**
- Tablet: left panel = list, right panel = detail (side-by-side)
- Phone: push detail screen onto the navigation stack

### 9.2 flutter_screenutil

**Package:** `flutter_screenutil`

Scales font sizes and spacing with screen density. Initialize once per design size:

```dart
ScreenUtil.init(context, designSize: const Size(390, 844));
// Then throughout the app:
Text('Hello', style: TextStyle(fontSize: 16.sp))
SizedBox(height: 24.h, width: 48.w)
```

---

## 10. HTTP Client — Dio

**Packages:** `dio` + `cookie_jar` + `dio_cookie_manager`

**Why Dio:**
- Interceptors for auth token refresh on 401 (equivalent to Axios interceptors in the web app)
- Multipart file uploads for transaction attachments
- Cancel tokens for aborting in-flight requests on navigation
- `PersistCookieJar` for session cookie handling (mirrors browser cookie behavior)
- Detailed error types via `DioException.type`

**Why not the `http` package:**
Lacks interceptors, cookie management, and cancel tokens. For a full app with auth refresh and
file uploads, Dio is the correct choice.

```dart
final dio = Dio(BaseOptions(
  baseUrl: 'https://api.kakeibo.app/api',
  connectTimeout: const Duration(seconds: 10),
  receiveTimeout: const Duration(seconds: 30),
));

final cookieJar = PersistCookieJar(
  storage: FileStorage('${appDocDir.path}/.cookies/'),
);
dio.interceptors.add(CookieManager(cookieJar));
dio.interceptors.add(AuthInterceptor(dio, ref)); // refresh token on 401
```

---

## 11. Data Classes — freezed + json_serializable

**Packages:** `freezed` + `freezed_annotation` + `json_serializable` + `json_annotation`

**Why freezed:**
- Generates immutable data classes with `copyWith`, `==`, `hashCode`, `toString`
- Sealed classes for discriminated unions (perfect for `AsyncValue<T>` and transaction type unions)
- Identical mental model to TypeScript `readonly` interfaces used in Pinia stores
- `fromJson`/`toJson` via `json_serializable` integration

Every domain model (Wallet, Transaction, Category, Budget, Goal, RecurringPattern, Notification,
Friend, etc.) uses freezed.

```dart
@freezed
class Budget with _$Budget {
  const factory Budget({
    required String id,
    required String name,
    required String categoryId,
    required String categoryName,
    required double limit,
    required String startDate,
    required String endDate,
    String? walletId,
    String? walletName,
    required double currentSpending,
    required String createdAt,
  }) = _Budget;

  factory Budget.fromJson(Map<String, dynamic> json) => _$BudgetFromJson(json);
}
```

---

## 12. Forms & Validation — reactive_forms

**Package:** `reactive_forms`

**Why reactive_forms:**
- Closest Flutter equivalent to VeeValidate + Zod used in the web app
- Declarative, model-driven forms (FormGroup + FormControl)
- Built-in validators: required, email, minLength, maxLength, pattern
- Custom validators for amount range and date constraints
- `ReactiveFormBuilder` widget — no manual `TextEditingController` management
- Type-safe: `FormControl<double>` vs `FormControl<String>`

**Why not alternatives:**
- **flutter_form_builder**: Similar but less type-safe API.
- **Manual TextEditingController**: Significant boilerplate per field.

```dart
// Mirrors TransactionForm.vue
final form = FormGroup({
  'type': FormControl<String>(
    value: 'Expense',
    validators: [Validators.required],
  ),
  'amount': FormControl<double>(validators: [
    Validators.required,
    Validators.min(0.01),
    Validators.max(999999999.99),
  ]),
  'description': FormControl<String>(
    validators: [Validators.maxLength(500)],
  ),
  'date': FormControl<DateTime>(
    value: DateTime.now(),
    validators: [Validators.required],
  ),
  'categoryId': FormControl<String>(validators: [Validators.required]),
});
```

---

## 13. Localization — easy_localization

**Package:** `easy_localization`

**Why easy_localization:**
- Reads JSON locale files directly — the existing `locales/en.json` and `locales/es.json` from
  the web app can be copied as-is into Flutter assets
- Simple API: `'wallets.title'.tr()` — matches the existing nested JSON key structure
- Plural forms, gender, and context variants supported
- Lazy locale loading (only loads the active locale)
- Runtime locale switching via `context.setLocale(Locale('es'))`

**Why not flutter_localizations + intl (official approach):**
- Requires `.arb` files (different format from the project's `.json` locales)
- Requires code generation (`flutter gen-l10n`)
- More setup for the same outcome
- Cannot share locale files with the web app

```dart
EasyLocalization(
  supportedLocales: [Locale('en'), Locale('es')],
  path: 'assets/locales',  // copy from src/Kakeibo.App/locales/
  fallbackLocale: Locale('en'),
  child: MyApp(),
)
```

---

## 14. Currency & Date Formatting — intl

**Package:** `intl` (transitive dependency of easy_localization)

- `NumberFormat.currency(symbol: '$', decimalDigits: 2)` — handles `currencyDisplay`,
  `currencySymbolPosition`, `currencyDecimalSeparator`, `currencyGroupSeparator` user preferences
- `DateFormat('MMM d, yyyy').format(date)` — locale-aware date formatting

No additional package needed.

---

## 15. Storage

### 15.1 Sensitive data — flutter_secure_storage

**Package:** `flutter_secure_storage`

Stores sensitive values in Keychain (iOS) and EncryptedSharedPreferences/Keystore (Android).
Used for: JWT access token, user ID, any sensitive cached value.

### 15.2 Preferences — shared_preferences

**Package:** `shared_preferences`

Non-sensitive user preferences: theme mode (light/dark/system), language override, onboarding
completed flag, dashboard period toggle (7d/30d/90d).

---

## 16. Push Notifications — firebase_messaging

**Packages:** `firebase_core` + `firebase_messaging` + `flutter_local_notifications`

**Why Firebase Messaging:**
- FCM (Firebase Cloud Messaging) is the universal delivery mechanism for both Android (native FCM)
  and iOS (APNs via FCM)
- The backend already has a push notification infrastructure (`IWebPushService` with VAPID keys).
  Adding a FCM adapter allows the same event handlers to send to both web browsers (Web Push) and
  mobile devices (FCM) — minimal backend change
- Free tier covers typical usage

**flutter_local_notifications handles:**
- Foreground notification display (app is open)
- Notification tap → deep link to the relevant screen via GoRouter
- Notification channels (Android 8+)

---

## 17. Image Handling

**Packages:** `cached_network_image` + `image_picker`

- `cached_network_image`: Network avatars with caching, fade-in animation, error fallback.
  The standard for network images in Flutter.
- `image_picker`: Camera or gallery picker for avatar upload (mirrors the web app's avatar
  upload in ProfileForm.vue).

---

## 18. File Handling (Import/Export)

**Packages:** `file_picker` + `path_provider` + `open_filex`

- `file_picker`: User picks `.sqlite` or `.csv` files for import (mirrors ImportExportSection.vue)
- `path_provider`: Gets the app's document directory for saving exported files
- `open_filex`: Opens the exported file with the system handler (share sheet / file manager)

---

## 19. Date Picker — calendar_date_picker2

**Package:** `calendar_date_picker2`

Beautiful, customizable date picker with single date, multi-date, and date range modes.

Used for: transaction date, budget date range, goal deadline, recurring pattern dates, activity
feed date filters.

Material 3's `showDatePicker` is functional but lacks date range support. `calendar_date_picker2`
fills that gap.

---

## 20. Miscellaneous Utilities

| Package | Purpose | Justification |
|---------|---------|---------------|
| `url_launcher` | Open external links (invitation emails, docs) | Standard Flutter package |
| `share_plus` | Share exported files via system share sheet | Official FlutterFavorite |
| `package_info_plus` | App version in settings screen | Official FlutterFavorite |
| `connectivity_plus` | Detect offline, show banner | Official FlutterFavorite |
| `shimmer` | Loading skeleton placeholders (mirrors Vue `Skeleton` component) | Better UX than spinners |
| `infinite_scroll_pagination` | Paginated transaction and activity lists (50/100 per page) | Purpose-built for this use case |
| `flutter_slidable` | Swipe-to-delete/edit on list items | Standard mobile UX pattern |
| `timeago` | Relative time ("2 hours ago") for notifications and activity feed | Same pattern as web app |

---

## 21. Testing

| Package | Purpose |
|---------|---------|
| `flutter_test` (built-in) | Widget tests, unit tests |
| Riverpod `ProviderContainer` | Test notifiers in isolation without Flutter widgets |
| `mocktail` | Mock repositories and services. No code generation required (unlike mockito). |
| `patrol` | E2E tests with native interactions (more reliable than `integration_test` alone) |

**Strategy:**
- **Unit**: domain models, repository logic, notifier state transitions
- **Widget**: individual screens with mocked providers
- **E2E**: critical flows — login → create wallet → record transaction → check dashboard

---

## 22. Code Generation

All generators run via `build_runner`:

```bash
dart run build_runner build --delete-conflicting-outputs
dart run build_runner watch   # during development
```

| Generator | Annotation | Output |
|-----------|-----------|--------|
| `freezed` | `@freezed` | Immutable data classes |
| `json_serializable` | `@JsonSerializable` | `fromJson` / `toJson` |
| `riverpod_generator` | `@riverpod` | Type-safe providers |
| `go_router_builder` | `@TypedGoRoute` | Type-safe route helpers |

---

## 23. Complete pubspec.yaml

```yaml
dependencies:
  flutter:
    sdk: flutter

  # State management
  flutter_riverpod: ^2.6.1
  riverpod_annotation: ^2.6.1

  # Navigation
  go_router: ^14.6.2

  # UI & Theming
  flex_color_scheme: ^8.0.2
  flutter_adaptive_scaffold: ^0.2.3
  flutter_screenutil: ^5.9.3

  # Charts
  fl_chart: ^0.69.0

  # Icons
  lucide_icons: ^0.3.6
  flutter_svg: ^2.0.10+1

  # HTTP & Networking
  dio: ^5.7.0
  cookie_jar: ^4.0.8
  dio_cookie_manager: ^3.1.1

  # Data classes
  freezed_annotation: ^2.4.4
  json_annotation: ^4.9.0

  # Forms
  reactive_forms: ^17.0.0

  # Localization
  easy_localization: ^3.0.7+1

  # Formatting
  intl: ^0.19.0

  # Storage
  flutter_secure_storage: ^9.2.2
  shared_preferences: ^2.3.3

  # Push notifications
  firebase_core: ^3.8.1
  firebase_messaging: ^15.1.6
  flutter_local_notifications: ^18.0.1

  # Images & Files
  cached_network_image: ^3.4.1
  image_picker: ^1.1.2
  file_picker: ^8.1.6
  path_provider: ^2.1.5
  open_filex: ^4.6.0

  # Date picker
  calendar_date_picker2: ^1.1.4

  # Utilities
  url_launcher: ^6.3.1
  share_plus: ^10.1.2
  package_info_plus: ^8.1.2
  connectivity_plus: ^6.1.1
  shimmer: ^3.0.0
  infinite_scroll_pagination: ^4.1.0
  flutter_slidable: ^3.1.1
  timeago: ^3.7.0

dev_dependencies:
  flutter_test:
    sdk: flutter

  # Code generation
  build_runner: ^2.4.14
  freezed: ^2.5.7
  json_serializable: ^6.9.0
  riverpod_generator: ^2.6.1
  go_router_builder: ^2.7.1

  # Testing
  mocktail: ^1.0.4
  patrol: ^3.12.1
```

---

## 24. Technologies Explicitly Excluded

| Technology | Reason |
|-----------|--------|
| **GetX** | All-in-one framework. Mixes routing, state, and DI. Hard to test. Not composable. |
| **BLoC** | 3× more boilerplate than Riverpod. Events + states + bloc class per feature. Overkill. |
| **Provider** | Superseded by Riverpod. Not type-safe. Lacks scoping. |
| **Hive / Isar** | Full local database. The app is API-first. `shared_preferences` is sufficient for preferences. |
| **sqflite** | Same reason. The API owns all data. Local DB adds complexity without MVP benefit. |
| **MobX** | Observable annotations everywhere. Less idiomatic than Riverpod in Flutter. |
| **Auto Route** | Valid but GoRouter is official, better documented, identical capability. |
| **syncfusion_flutter_charts** | Enterprise license. Attribution required. `fl_chart` is MIT. |
| **Supabase Flutter SDK** | Project uses custom .NET API, not Supabase. |
| **Firebase Firestore** | Same reason. Firebase is used only for FCM push delivery. |
| **flutter_hooks** | Adds React-like hooks. Riverpod handles side effects cleanly. Not needed. |
| **getwidget** | Generic widget library. Material 3 + flex_color_scheme is better. |

---

## 25. Tablet Layout Strategy

`AdaptiveScaffold` from `flutter_adaptive_scaffold` handles navigation adaptation automatically:

| Screen Width | Navigation Component | Equivalent Web Component |
|-------------|---------------------|--------------------------|
| < 600dp | `BottomNavigationBar` (4–5 tabs) | — (mobile-only pattern) |
| 600–840dp | `NavigationRail` (icons + labels) | Collapsed AppSidebar |
| ≥ 840dp | `NavigationDrawer` (full labels + icons) | Expanded AppSidebar |

**List-detail screens on tablet** (WalletDetail, TransactionList):
- Tablet: `Row` with fixed-width list panel (320dp) + `Expanded` detail panel
- Phone: standard push navigation (list → detail screen)

`GoRouter`'s `StatefulShellRoute` preserves each tab's navigation stack independently, exactly
as Vue Router's nested routes do.

---

## 26. Summary Table

| Concern | Decision | Package(s) |
|---------|----------|-----------|
| State management | Riverpod + code generation | `flutter_riverpod`, `riverpod_generator` |
| Navigation | GoRouter | `go_router` |
| Theming | Material 3 + FlexColorScheme | `flex_color_scheme` |
| Charts | fl_chart | `fl_chart` |
| Icons | Lucide Icons | `lucide_icons` |
| Adaptive navigation | AdaptiveScaffold | `flutter_adaptive_scaffold` |
| Density scaling | ScreenUtil | `flutter_screenutil` |
| HTTP client | Dio + Cookie Manager | `dio`, `dio_cookie_manager` |
| Data models | Freezed + JSON serializable | `freezed`, `json_serializable` |
| Forms & validation | Reactive Forms | `reactive_forms` |
| Localization | Easy Localization | `easy_localization` |
| Currency/Date format | intl | `intl` |
| Secure storage | FlutterSecureStorage | `flutter_secure_storage` |
| App preferences | SharedPreferences | `shared_preferences` |
| Push notifications | Firebase Messaging + local notifications | `firebase_messaging`, `flutter_local_notifications` |
| Network images | CachedNetworkImage | `cached_network_image` |
| Image picker | ImagePicker | `image_picker` |
| File import/export | FilePicker + PathProvider + OpenFilex | `file_picker`, `path_provider`, `open_filex` |
| Date picker | calendar_date_picker2 | `calendar_date_picker2` |
| Pagination | InfiniteScrollPagination | `infinite_scroll_pagination` |
| List swipe actions | FlutterSlidable | `flutter_slidable` |
| Loading states | Shimmer | `shimmer` |
| Relative time | Timeago | `timeago` |
| Mocking in tests | Mocktail | `mocktail` |
| E2E testing | Patrol | `patrol` |

---

*Study date: 2026-03-08*
*Scope: full Kakeibo.App feature set (37 screens, 13 stores, ~124 components)*
*Target: Flutter 3.27+, Dart 3.6+, Android + iOS*
