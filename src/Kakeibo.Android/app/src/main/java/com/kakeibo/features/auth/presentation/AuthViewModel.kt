package com.kakeibo.features.auth.presentation

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.kakeibo.core.api.ApiResult
import com.kakeibo.features.auth.data.AuthRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed interface AuthEvent {
    data object LoginSuccess : AuthEvent
    data object RegisterSuccess : AuthEvent
    data object ForgotPasswordSuccess : AuthEvent
    data object ResetPasswordSuccess : AuthEvent
    data object VerifyEmailSuccess : AuthEvent
}

@HiltViewModel
class AuthViewModel @Inject constructor(
    private val repository: AuthRepository,
) : ViewModel() {

    private val _uiState = MutableStateFlow<AuthUiState>(AuthUiState.Idle)
    val uiState = _uiState.asStateFlow()

    private val _loginForm = MutableStateFlow(LoginFormState())
    val loginForm = _loginForm.asStateFlow()

    private val _registerForm = MutableStateFlow(RegisterFormState())
    val registerForm = _registerForm.asStateFlow()

    private val _events = Channel<AuthEvent>()
    val events = _events.receiveAsFlow()

    fun updateLoginEmail(value: String) {
        _loginForm.value = _loginForm.value.copy(email = value, emailError = null)
    }

    fun updateLoginPassword(value: String) {
        _loginForm.value = _loginForm.value.copy(password = value, passwordError = null)
    }

    fun updateRegisterEmail(value: String) {
        _registerForm.value = _registerForm.value.copy(email = value, emailError = null)
    }

    fun updateRegisterPassword(value: String) {
        _registerForm.value = _registerForm.value.copy(password = value, passwordError = null)
    }

    fun updateRegisterConfirmPassword(value: String) {
        _registerForm.value = _registerForm.value.copy(confirmPassword = value, confirmPasswordError = null)
    }

    fun updateRegisterCurrency(value: String) {
        _registerForm.value = _registerForm.value.copy(currency = value)
    }

    fun login() {
        val form = _loginForm.value
        if (!validateLoginForm(form)) return
        viewModelScope.launch {
            _uiState.value = AuthUiState.Loading
            when (val result = repository.login(form.email.trim(), form.password)) {
                is ApiResult.Success -> {
                    _uiState.value = AuthUiState.Success
                    _events.send(AuthEvent.LoginSuccess)
                }
                is ApiResult.Error -> _uiState.value = AuthUiState.Error(result.message)
                is ApiResult.NetworkError -> _uiState.value = AuthUiState.Error("Network error. Check your connection.")
            }
        }
    }

    fun register() {
        val form = _registerForm.value
        if (!validateRegisterForm(form)) return
        viewModelScope.launch {
            _uiState.value = AuthUiState.Loading
            when (val result = repository.register(form.email.trim(), form.password, form.currency)) {
                is ApiResult.Success -> {
                    _uiState.value = AuthUiState.Success
                    _events.send(AuthEvent.RegisterSuccess)
                }
                is ApiResult.Error -> _uiState.value = AuthUiState.Error(result.message)
                is ApiResult.NetworkError -> _uiState.value = AuthUiState.Error("Network error. Check your connection.")
            }
        }
    }

    fun forgotPassword(email: String) {
        if (email.isBlank()) return
        viewModelScope.launch {
            _uiState.value = AuthUiState.Loading
            when (val result = repository.forgotPassword(email.trim())) {
                is ApiResult.Success -> {
                    _uiState.value = AuthUiState.Success
                    _events.send(AuthEvent.ForgotPasswordSuccess)
                }
                is ApiResult.Error -> _uiState.value = AuthUiState.Error(result.message)
                is ApiResult.NetworkError -> _uiState.value = AuthUiState.Error("Network error. Check your connection.")
            }
        }
    }

    fun resetPassword(token: String, newPassword: String) {
        viewModelScope.launch {
            _uiState.value = AuthUiState.Loading
            when (val result = repository.resetPassword(token, newPassword)) {
                is ApiResult.Success -> {
                    _uiState.value = AuthUiState.Success
                    _events.send(AuthEvent.ResetPasswordSuccess)
                }
                is ApiResult.Error -> _uiState.value = AuthUiState.Error(result.message)
                is ApiResult.NetworkError -> _uiState.value = AuthUiState.Error("Network error. Check your connection.")
            }
        }
    }

    fun verifyEmail(token: String) {
        viewModelScope.launch {
            _uiState.value = AuthUiState.Loading
            when (val result = repository.verifyEmail(token)) {
                is ApiResult.Success -> {
                    _uiState.value = AuthUiState.Success
                    _events.send(AuthEvent.VerifyEmailSuccess)
                }
                is ApiResult.Error -> _uiState.value = AuthUiState.Error(result.message)
                is ApiResult.NetworkError -> _uiState.value = AuthUiState.Error("Network error. Check your connection.")
            }
        }
    }

    fun clearError() {
        if (_uiState.value is AuthUiState.Error) _uiState.value = AuthUiState.Idle
    }

    private fun validateLoginForm(form: LoginFormState): Boolean {
        var valid = true
        var updated = form
        if (form.email.isBlank()) { updated = updated.copy(emailError = "Email is required"); valid = false }
        if (form.password.isBlank()) { updated = updated.copy(passwordError = "Password is required"); valid = false }
        _loginForm.value = updated
        return valid
    }

    private fun validateRegisterForm(form: RegisterFormState): Boolean {
        var valid = true
        var updated = form
        if (form.email.isBlank()) { updated = updated.copy(emailError = "Email is required"); valid = false }
        if (form.password.length < 8) { updated = updated.copy(passwordError = "Password must be at least 8 characters"); valid = false }
        if (form.password != form.confirmPassword) { updated = updated.copy(confirmPasswordError = "Passwords do not match"); valid = false }
        _registerForm.value = updated
        return valid
    }
}
