# Kakeibo ProGuard / R8 rules
# R8 is enabled by default in release builds (isMinifyEnabled = true)

# ── kotlinx.serialization ────────────────────────────────────────────────────
# Keep all @Serializable classes so R8 does not strip them
-keepattributes *Annotation*, InnerClasses
-dontnote kotlinx.serialization.AnnotationsKt
-keepclassmembers class kotlinx.serialization.json.** {
    *** Companion;
}
-keepclasseswithmembers class **$$serializer {
    *;
}

# ── Retrofit ─────────────────────────────────────────────────────────────────
# Keep all Retrofit API interfaces
-keep,allowobfuscation,allowshrinking interface retrofit2.Call
-keep,allowobfuscation,allowshrinking class retrofit2.Response
-keep,allowobfuscation,allowshrinking class kotlin.coroutines.Continuation

# ── Hilt ─────────────────────────────────────────────────────────────────────
# Hilt adds its own consumer ProGuard rules via AAR — no manual rules needed.

# ── Vico ─────────────────────────────────────────────────────────────────────
# Keep Vico chart model classes used via reflection in animation
-keep class com.patrykandpatrick.vico.** { *; }

# ── Material Icons Extended ───────────────────────────────────────────────────
# R8 strips unused icons automatically — no explicit keep rules needed.

# ── OkHttp / Okio ────────────────────────────────────────────────────────────
-dontwarn okhttp3.**
-dontwarn okio.**
-dontwarn javax.annotation.**
