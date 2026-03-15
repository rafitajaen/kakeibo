package com.kakeibo.features.auth.domain

import kotlinx.serialization.Serializable

@Serializable
data class User(
    val id: String,
    val email: String,
    val username: String,
    val currency: String,
    val role: String,
    val isEmailVerified: Boolean,
    val isOnboardingComplete: Boolean,
)
