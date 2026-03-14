plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
    alias(libs.plugins.kotlin.compose)
    alias(libs.plugins.kotlin.serialization)
    alias(libs.plugins.hilt)
    alias(libs.plugins.ksp)
    alias(libs.plugins.google.services)
}

android {
    namespace = "com.kakeibo"
    compileSdk = 36
    buildToolsVersion = "36.1.0"

    defaultConfig {
        applicationId = "com.kakeibo.app"
        minSdk = 26
        targetSdk = 36
        versionCode = 1
        versionName = "1.0.0"

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"

        buildConfigField("String", "API_BASE_URL", "\"http://10.0.2.2:5000/api/\"")
    }

    buildTypes {
        release {
            isMinifyEnabled = true
            isShrinkResources = true
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro",
            )
            buildConfigField("String", "API_BASE_URL", "\"https://api.kakeibo.app/api/\"")
        }
        debug {
            isMinifyEnabled = false
            applicationIdSuffix = ".debug"
            versionNameSuffix = "-debug"
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = "17"
    }

    buildFeatures {
        compose = true
        buildConfig = true
    }

    packaging {
        resources {
            excludes += "/META-INF/{AL2.0,LGPL2.1}"
        }
    }
}

dependencies {
    // Compose BOM — manages all Compose library versions consistently
    val composeBom = platform(libs.compose.bom)
    implementation(composeBom)
    implementation(libs.compose.ui)
    implementation(libs.compose.ui.tooling.preview)
    implementation(libs.compose.material3)
    implementation(libs.compose.material.icons.extended)

    // Compose + Activity + Lifecycle
    implementation(libs.activity.compose)
    implementation(libs.lifecycle.viewmodel.compose)
    implementation(libs.lifecycle.runtime.compose)

    // Navigation
    implementation(libs.navigation.compose)

    // Material 3 Adaptive (adaptive scaffold, WindowSizeClass)
    implementation(libs.adaptive)
    implementation(libs.adaptive.navigation)
    implementation(libs.adaptive.layout)

    // Dependency Injection — Hilt
    implementation(libs.hilt.android)
    ksp(libs.hilt.android.compiler)
    implementation(libs.hilt.navigation.compose)

    // Networking — Retrofit + OkHttp
    implementation(libs.retrofit)
    implementation(libs.okhttp)
    implementation(libs.okhttp.logging.interceptor)
    implementation(libs.retrofit.kotlinx.serialization.converter)

    // JSON Serialization — kotlinx.serialization
    implementation(libs.kotlinx.serialization.json)

    // Charts — Vico (Compose-native, Material 3)
    implementation(libs.vico.compose.m3)

    // Images — Coil 3 (Kotlin-native, Compose-native)
    implementation(libs.coil.compose)
    implementation(libs.coil.network.okhttp)

    // Local Storage — Room (offline cache)
    implementation(libs.room.runtime)
    implementation(libs.room.ktx)
    ksp(libs.room.compiler)

    // User Preferences — DataStore
    implementation(libs.datastore.preferences)

    // Secure Token Storage — EncryptedSharedPreferences
    implementation(libs.security.crypto)

    // Pagination — Paging 3
    implementation(libs.paging.runtime.ktx)
    implementation(libs.paging.compose)

    // Push Notifications — Firebase Cloud Messaging
    implementation(platform(libs.firebase.bom))
    implementation(libs.firebase.messaging.ktx)

    // Date/Time — kotlinx-datetime (equivalent to NodaTime used in API)
    implementation(libs.kotlinx.datetime)

    // Splash screen
    implementation(libs.core.splashscreen)

    // Runtime permissions in Compose
    implementation(libs.accompanist.permissions)

    // Shimmer loading effect
    implementation(libs.compose.shimmer)

    // Structured logging
    implementation(libs.timber)

    // --- Testing ---
    testImplementation(libs.junit)
    testImplementation(libs.kotlinx.coroutines.test)
    testImplementation(libs.mockk)
    testImplementation(libs.truth)

    androidTestImplementation(composeBom)
    androidTestImplementation(libs.compose.ui.test.junit4)
    androidTestImplementation(libs.mockwebserver)

    debugImplementation(libs.compose.ui.tooling)
    debugImplementation(libs.compose.ui.test.manifest)
}
