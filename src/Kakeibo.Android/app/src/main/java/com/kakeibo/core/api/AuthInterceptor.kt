package com.kakeibo.core.api

import com.kakeibo.core.auth.TokenStore
import okhttp3.Interceptor
import okhttp3.Response
import javax.inject.Inject
import javax.inject.Singleton

/**
 * OkHttp interceptor that attaches the Bearer token to every request.
 *
 * Mirrors the Axios interceptor in the web app's authStore.
 * On 401 response, the token refresh logic should be triggered here.
 */
@Singleton
class AuthInterceptor @Inject constructor(
    private val tokenStore: TokenStore,
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

        // TODO: implement token refresh on 401
        // if (response.code == 401) { ... }

        return response
    }
}
