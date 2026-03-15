package com.kakeibo.core.api

import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.asSharedFlow
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Singleton that signals session expiry to the UI layer.
 * AuthInterceptor emits to this when it receives a 401 that cannot be recovered by refresh.
 * MainActivity observes [tokenExpiredFlow] and navigates to the login screen.
 */
@Singleton
class SessionManager @Inject constructor() {

    private val _tokenExpiredFlow = MutableSharedFlow<Unit>(extraBufferCapacity = 1)
    val tokenExpiredFlow = _tokenExpiredFlow.asSharedFlow()

    fun onTokenExpired() {
        _tokenExpiredFlow.tryEmit(Unit)
    }
}
