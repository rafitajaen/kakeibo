# Kakeibo Android

Native Android app for the Kakeibo personal finance platform.
Built with Kotlin 2.x + Jetpack Compose + Material 3.

## Quick Start

See [`.claude/guides/android/00-setup.md`](../../.claude/guides/android/00-setup.md)
for a step-by-step setup guide (Android SDK installation, emulator, first build).

## Prerequisites

- Java 17+
- Android SDK (API 26+ platform, build-tools 35)
- `local.properties` with `sdk.dir` pointing to your Android SDK

## Build

```bash
cd src/Kakeibo.Android
./gradlew assembleDebug      # Debug APK
./gradlew assembleRelease    # Release APK (requires signing config)
./gradlew test               # Unit tests (no device)
./gradlew connectedCheck     # Instrumented tests (requires running device/emulator)
./gradlew lint               # Lint
```

## Architecture

MVVM + Unidirectional Data Flow + Feature Vertical Slices.
See `CLAUDE.md` in this directory for the full tech stack and conventions.

## API

The app connects to `Kakeibo.Api` at `http://10.0.2.2:5000` (emulator) or
`https://api.kakeibo.app` (production). Configure in `app/build.gradle.kts`.
