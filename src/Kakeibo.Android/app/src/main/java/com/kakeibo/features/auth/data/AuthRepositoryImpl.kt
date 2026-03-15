package com.kakeibo.features.auth.data

import com.kakeibo.core.api.ApiResult
import com.kakeibo.core.api.safeApiCall
import com.kakeibo.core.auth.TokenStore
import com.kakeibo.features.auth.domain.User
import javax.inject.Inject

class AuthRepositoryImpl @Inject constructor(
    private val api: AuthApi,
    private val tokenStore: TokenStore,
) : AuthRepository {

    override suspend fun login(email: String, password: String): ApiResult<User> {
        return when (val result = safeApiCall { api.login(LoginRequest(email, password)) }) {
            is ApiResult.Success -> {
                tokenStore.accessToken = result.data.accessToken
                tokenStore.refreshToken = result.data.refreshToken
                val userResult = safeApiCall { api.me() }
                if (userResult is ApiResult.Success) {
                    tokenStore.currency = userResult.data.currency
                }
                userResult
            }
            is ApiResult.Error -> result
            is ApiResult.NetworkError -> result
        }
    }

    override suspend fun register(email: String, password: String, currency: String): ApiResult<Unit> {
        return when (val result = safeApiCall { api.register(RegisterRequest(email, password, currency)) }) {
            is ApiResult.Success -> {
                tokenStore.accessToken = result.data.accessToken
                tokenStore.refreshToken = result.data.refreshToken
                tokenStore.currency = currency
                ApiResult.Success(Unit)
            }
            is ApiResult.Error -> result
            is ApiResult.NetworkError -> result
        }
    }

    override suspend fun getCurrentUser(): ApiResult<User> = safeApiCall { api.me() }

    override suspend fun logout(): ApiResult<Unit> {
        val result = safeApiCall { api.logout() }
        tokenStore.clear()
        return result
    }

    override suspend fun forgotPassword(email: String): ApiResult<Unit> =
        safeApiCall { api.forgotPassword(ForgotPasswordRequest(email)) }

    override suspend fun resetPassword(token: String, newPassword: String): ApiResult<Unit> =
        safeApiCall { api.resetPassword(ResetPasswordRequest(token, newPassword)) }

    override suspend fun verifyEmail(token: String): ApiResult<Unit> =
        safeApiCall { api.verifyEmail(VerifyEmailRequest(token)) }

    override fun clearTokens() = tokenStore.clear()
}
