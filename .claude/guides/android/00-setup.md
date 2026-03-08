# Android Development Setup Guide

> Step-by-step guide for setting up the Kakeibo Android development environment
> from scratch, without Android Studio. Covers Linux, macOS, and Windows.

---

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Install Android SDK Command-Line Tools](#2-install-android-sdk-command-line-tools)
3. [Configure Environment Variables](#3-configure-environment-variables)
4. [Install Required SDK Components](#4-install-required-sdk-components)
5. [Configure the Project](#5-configure-the-project)
6. [Set up Firebase (FCM)](#6-set-up-firebase-fcm)
7. [First Build](#7-first-build)
8. [Run on Emulator](#8-run-on-emulator)
9. [Run on Physical Device](#9-run-on-physical-device)
10. [Open in Android Studio (Recommended)](#10-open-in-android-studio-recommended)
11. [Architecture Quick-Reference](#11-architecture-quick-reference)
12. [Useful Gradle Tasks](#12-useful-gradle-tasks)
13. [Troubleshooting](#13-troubleshooting)

---

## 1. Prerequisites

| Tool | Minimum version | Check |
|------|----------------|-------|
| Java (JDK) | 17 | `java -version` |
| Git | Any recent | `git --version` |
| Internet connection | — | For Gradle + SDK downloads |

Java 17 is the minimum. Java 21 or 22 also works. The Gradle wrapper handles its own
download — no system-wide Gradle install is needed.

### Install Java 17 (if missing)

**Linux (Ubuntu/Debian):**
```bash
sudo apt update && sudo apt install -y openjdk-17-jdk
# Verify
java -version
```

**macOS:**
```bash
brew install openjdk@17
# Then follow brew's instructions to link it
```

**Windows:** Download the JDK 17 installer from https://adoptium.net and run it.

---

## 2. Install Android SDK Command-Line Tools

You need the Android SDK but **not** Android Studio. The command-line tools are ~135 MB.

### Step 1: Download

Go to: https://developer.android.com/studio#command-line-tools-only

Download the zip for your platform:
- Linux: `commandlinetools-linux-*_latest.zip`
- macOS: `commandlinetools-mac-*_latest.zip`
- Windows: `commandlinetools-win-*_latest.zip`

Or download via curl (Linux — check the page for the latest version number):
```bash
# Find the latest URL at https://developer.android.com/studio#command-line-tools-only
# Example (update the version number):
curl -L "https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip" \
     -o /tmp/cmdline-tools.zip
```

### Step 2: Extract

The zip must be extracted so that the tools are at `cmdline-tools/latest/`:

**Linux / macOS:**
```bash
mkdir -p ~/Android/cmdline-tools
unzip /tmp/cmdline-tools.zip -d ~/Android/cmdline-tools/
# The zip extracts to a folder named "cmdline-tools" — rename it to "latest"
mv ~/Android/cmdline-tools/cmdline-tools ~/Android/cmdline-tools/latest
```

**Windows (PowerShell):**
```powershell
New-Item -ItemType Directory -Force "$env:USERPROFILE\Android\cmdline-tools"
Expand-Archive cmdline-tools.zip -DestinationPath "$env:USERPROFILE\Android\cmdline-tools"
Rename-Item "$env:USERPROFILE\Android\cmdline-tools\cmdline-tools" `
            "$env:USERPROFILE\Android\cmdline-tools\latest"
```

The final layout must be:
```
~/Android/
  cmdline-tools/
    latest/
      bin/
        sdkmanager
        avdmanager
      lib/
```

---

## 3. Configure Environment Variables

### Linux / macOS — add to `~/.bashrc` or `~/.zshrc`:

```bash
export ANDROID_HOME="$HOME/Android/Sdk"
export PATH="$ANDROID_HOME/cmdline-tools/latest/bin:$PATH"
export PATH="$ANDROID_HOME/platform-tools:$PATH"
export PATH="$ANDROID_HOME/emulator:$PATH"
```

Then reload:
```bash
source ~/.bashrc   # or source ~/.zshrc
```

### Windows — System Environment Variables:

1. Open **System Properties → Advanced → Environment Variables**
2. Add or update:
   - `ANDROID_HOME` = `C:\Users\YOUR_USERNAME\Android\Sdk`
   - Append to `Path`: `%ANDROID_HOME%\cmdline-tools\latest\bin`
   - Append to `Path`: `%ANDROID_HOME%\platform-tools`
   - Append to `Path`: `%ANDROID_HOME%\emulator`
3. Restart terminal / IDE

### Verify:
```bash
sdkmanager --version   # Should print a version number like "13.0"
```

---

## 4. Install Required SDK Components

```bash
# Accept all licences (required before installing components)
yes | sdkmanager --licenses

# Install required components
sdkmanager \
  "platform-tools" \
  "platforms;android-35" \
  "build-tools;35.0.0" \
  "emulator" \
  "system-images;android-35;google_apis;x86_64"
```

**What each component is:**

| Component | Purpose |
|-----------|---------|
| `platform-tools` | `adb`, `fastboot` — required for device communication |
| `platforms;android-35` | Android 15 SDK (compileSdk = 35) |
| `build-tools;35.0.0` | Build toolchain (aapt2, dx/d8) |
| `emulator` | Android emulator binary |
| `system-images;android-35;google_apis;x86_64` | Emulator image with Google APIs |

The Gradle wrapper downloads Gradle itself on first build — no manual install needed.

---

## 5. Configure the Project

### Step 1: Create `local.properties`

```bash
cd src/Kakeibo.Android
cp local.properties.example local.properties
```

Edit `local.properties`:
```properties
sdk.dir=/home/YOUR_USERNAME/Android/Sdk
```

Use the actual path to your Android SDK:
- Linux: `/home/YOUR_USERNAME/Android/Sdk`
- macOS: `/Users/YOUR_USERNAME/Library/Android/sdk`
- Windows: `C\:\\Users\\YOUR_USERNAME\\AppData\\Local\\Android\\Sdk`

### Step 2: Make the Gradle wrapper executable (Linux / macOS)

```bash
chmod +x gradlew
```

### Step 3: Download the Gradle wrapper JAR

The `gradle-wrapper.jar` is committed to the repository. If missing for any reason:
```bash
# From inside src/Kakeibo.Android/
gradle wrapper --gradle-version 8.11.1
```

---

## 6. Set up Firebase (FCM)

Firebase Cloud Messaging is required for push notifications.

### Step 1: Create a Firebase project

1. Go to https://console.firebase.google.com
2. Click **Add project** → name it `kakeibo-android`
3. Disable Google Analytics (optional)
4. Click **Continue**

### Step 2: Register the Android app

1. In the Firebase console, click **Add app → Android**
2. Package name: `com.kakeibo.app`
3. App nickname: `Kakeibo`
4. Download `google-services.json`

### Step 3: Place the file

```bash
cp ~/Downloads/google-services.json src/Kakeibo.Android/app/google-services.json
```

The `app/google-services.json.example` file shows the expected structure.

> **Security:** `app/google-services.json` is in `.gitignore` — never commit it.

### Step 4: API integration

After a user logs in, the app must register the FCM token with the Kakeibo API.
The backend stores the token per session and uses it to send push notifications.
See `KakeiboFirebaseMessagingService.kt` for the `onNewToken` stub.

---

## 7. First Build

```bash
cd src/Kakeibo.Android

# Build a debug APK
./gradlew assembleDebug
```

First run downloads Gradle 8.10.2 (~140 MB) and all Maven dependencies (~500 MB).
Subsequent builds are cached and much faster.

**Expected output:**
```
BUILD SUCCESSFUL in Xs
2 actionable tasks: 2 executed
```

APK location: `app/build/outputs/apk/debug/app-debug.apk`

### Run unit tests (no device needed)
```bash
./gradlew test
```

### Check for lint errors
```bash
./gradlew lint
```

---

## 8. Run on Emulator

### Step 1: Create an AVD (Android Virtual Device)

```bash
# List available system images
avdmanager list target

# Create AVD named "Kakeibo_Pixel8"
avdmanager create avd \
  --name "Kakeibo_Pixel8" \
  --package "system-images;android-35;google_apis;x86_64" \
  --device "pixel_8"
```

### Step 2: Start the emulator

```bash
# List available AVDs
emulator -list-avds

# Launch emulator (replace with your AVD name)
emulator -avd Kakeibo_Pixel8 -no-snapshot &
```

Wait for the emulator to fully boot (the lock screen appears).

### Step 3: Install the APK

```bash
adb install app/build/outputs/apk/debug/app-debug.apk
```

Or use `./gradlew installDebug` which builds + installs in one step.

### Step 4: API connection from emulator

The Android emulator routes `10.0.2.2` to the host machine's `localhost`.
The debug `API_BASE_URL` in `build.gradle.kts` is set to `http://10.0.2.2:5000/api/`.

Start the Kakeibo API on your machine and the emulator will reach it automatically:
```bash
# In the kakeibo root:
bun run api:run
```

---

## 9. Run on Physical Device

### Step 1: Enable Developer Mode

1. Open **Settings → About phone**
2. Tap **Build number** seven times
3. Go back → **Settings → Developer options**
4. Enable **USB debugging**

### Step 2: Connect via USB

```bash
adb devices
# Should show your device (e.g., "emulator-5554  device" or "ABC123XYZ device")
```

### Step 3: Install

```bash
adb install app/build/outputs/apk/debug/app-debug.apk
```

### Step 4: API connection from physical device

`10.0.2.2` does not work on physical devices. Options:
- Use your machine's LAN IP: edit `API_BASE_URL` in `build.gradle.kts` to `http://192.168.x.x:5000/api/`
- Use ngrok or a similar tunnel for remote testing
- Use a release build pointing to the production API

---

## 10. Open in Android Studio (Recommended)

Android Studio provides:
- In-editor `@Preview` rendering for Composables
- Visual layout debugger
- Logcat with filtering
- APK analyser
- Profiler (CPU, memory, network)
- AVD Manager GUI

### Install Android Studio

Download from: https://developer.android.com/studio

During installation, Android Studio will offer to install the Android SDK — if you
followed this guide, point it to `~/Android/Sdk` (your existing installation) to
avoid a second download.

### Import the project

1. Open Android Studio
2. **File → Open**
3. Navigate to `src/Kakeibo.Android/`
4. Click **OK**
5. Android Studio detects the Gradle project and syncs automatically

### Configure SDK path

If Android Studio shows "SDK not found", go to:
**File → Project Structure → SDK Location** → set to your `~/Android/Sdk` path.

---

## 11. Architecture Quick-Reference

```
src/Kakeibo.Android/
  app/src/main/java/com/kakeibo/
    core/                    ← Shared infrastructure (DI, theme, navigation, auth)
      api/                   ← AuthInterceptor, ApiResult<T>
      auth/                  ← TokenStore (EncryptedSharedPreferences)
      db/                    ← KakeiboDatabase (Room)
      di/                    ← NetworkModule, DatabaseModule (Hilt)
      navigation/            ← Routes.kt (type-safe), AppNavGraph.kt
        shell/               ← AppShell.kt (NavigationSuiteScaffold)
      push/                  ← KakeiboFirebaseMessagingService
      theme/                 ← KakeiboTheme.kt, Typography.kt
    features/                ← One folder per domain
      auth/
        data/                ← AuthRepository.kt, AuthApi.kt
        domain/              ← User.kt, LoginRequest.kt
        presentation/        ← LoginScreen.kt, AuthViewModel.kt, AuthUiState.kt
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

**Adding a new screen:**
1. Define a route in `core/navigation/Routes.kt` (`@Serializable data object/class`)
2. Add `composable<YourRoute> { YourScreen(navController) }` in `AppNavGraph.kt`
3. Create `features/{domain}/presentation/YourScreen.kt`
4. Create `features/{domain}/presentation/YourViewModel.kt` (sealed UiState + StateFlow)
5. Create `features/{domain}/data/YourRepository.kt` + `YourApi.kt`
6. Create `features/{domain}/domain/YourModel.kt`

---

## 12. Useful Gradle Tasks

```bash
# Build
./gradlew assembleDebug          # Debug APK
./gradlew assembleRelease        # Release APK
./gradlew bundleRelease          # Release AAB (for Play Store)

# Install
./gradlew installDebug           # Build + install on connected device

# Test
./gradlew test                   # Unit tests (JVM, no device)
./gradlew connectedCheck         # Instrumented tests (requires device)
./gradlew testDebugUnitTest      # Unit tests for debug variant only

# Quality
./gradlew lint                   # Lint (all variants)
./gradlew lintDebug              # Lint (debug only)

# Dependency management
./gradlew dependencies           # Full dependency tree
./gradlew app:dependencies       # App module only
./gradlew dependencyUpdates      # Check for version updates (requires plugin)

# Clean
./gradlew clean                  # Delete build directories
```

---

## 13. Troubleshooting

### `sdk.dir` not found

```
SDK location not found. Define a valid SDK location with an ANDROID_HOME
environment variable or by setting the sdk.dir path in your project's local.properties file.
```

**Fix:** Ensure `local.properties` exists in `src/Kakeibo.Android/` with the correct `sdk.dir`.

---

### `license not accepted`

```
Failed to install the following Android SDK packages as some licences have not been accepted
```

**Fix:**
```bash
yes | sdkmanager --licenses
```

---

### `Could not resolve com.google.dagger:hilt-android`

```
FAILURE: Build failed with an exception.
Could not resolve com.google.dagger:hilt-android:2.52.
```

**Fix:** Ensure you have internet access and that the Google Maven repository is reachable.
If behind a corporate proxy, configure Gradle proxy settings in `~/.gradle/gradle.properties`.

---

### `google-services.json` missing

```
File google-services.json is missing. The Google Services Plugin cannot function without it.
```

**Fix:** Follow [Section 6](#6-set-up-firebase-fcm) to create and place `google-services.json`.
As a temporary workaround for local development without FCM, you can copy the example:
```bash
cp app/google-services.json.example app/google-services.json
```
This will build but FCM push notifications will not work until real values are provided.

---

### Emulator very slow

Enable hardware acceleration:
- **Linux:** Install KVM: `sudo apt install qemu-kvm && sudo usermod -aG kvm $USER`
- **macOS:** Enabled by default (Hypervisor.framework)
- **Windows:** Enable Hyper-V or HAXM in BIOS/UEFI settings

---

### `10.0.2.2` connection refused

The Kakeibo API is not running. Start it:
```bash
# From the kakeibo monorepo root:
bun run api:run
```

Also ensure the API listens on `0.0.0.0:5000`, not `localhost:5000`, so the emulator
can reach the host. Check `appsettings.json` → `Kestrel.Endpoints.Http.Url`.
