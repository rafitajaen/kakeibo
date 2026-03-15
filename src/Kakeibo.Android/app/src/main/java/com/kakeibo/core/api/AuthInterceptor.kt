package com.kakeibo.core.api

import com.kakeibo.core.auth.TokenStore
import okhttp3.Interceptor
import okhttp3.Response
import timber.log.Timber
import javax.inject.Inject
import javax.inject.Singleton

/**
 * OkHttp interceptor that attaches the Bearer token to every request.
 *
 * On 401, clears tokens and signals [SessionManager] so [MainActivity] can
 * navigate to the login screen. Token refresh is not implemented because the
 * API issues long-lived access tokens for MVP.
 */
@Singleton
class AuthInterceptor @Inject constructor(
    private val tokenStore: TokenStore,
    private val sessionManager: SessionManager,
) : Interceptor {

    override fun intercept(chain: Interceptor.Chain): Response {
        val token = tokenStore.accessToken
        val request = if (token != null) {
            chain.request().newBuilder()
                .addHeader("Authorization", "Bearer $token")
                .build()
        } else {
            chain.request()
        }

        val response = chain.proceed(request)

        if (response.code == 401) {
            Timber.w("401 Unauthorized — clearing tokens and signalling session expiry")
            tokenStore.clear()
            sessionManager.onTokenExpired()
        }

        return response
    }
}
