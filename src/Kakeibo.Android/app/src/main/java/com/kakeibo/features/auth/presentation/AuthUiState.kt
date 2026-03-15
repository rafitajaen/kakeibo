package com.kakeibo.features.auth.presentation

sealed interface AuthUiState {
    data object Idle : AuthUiState
    data object Loading : AuthUiState
    data class Error(val message: String) : AuthUiState
    data object Success : AuthUiState
}

data class LoginFormState(
    val email: String = "",
    val password: String = "",
    val emailError: String? = null,
    val passwordError: String? = null,
)

data class RegisterFormState(
    val email: String = "",
    val password: String = "",
    val confirmPassword: String = "",
    val currency: String = "USD",
    val emailError: String? = null,
    val passwordError: String? = null,
    val confirmPasswordError: String? = null,
)
