package com.kakeibo.features.auth.data

import com.kakeibo.core.api.ApiResult
import com.kakeibo.features.auth.domain.User

interface AuthRepository {
    suspend fun login(email: String, password: String): ApiResult<User>
    suspend fun register(email: String, password: String, currency: String): ApiResult<Unit>
    suspend fun getCurrentUser(): ApiResult<User>
    suspend fun logout(): ApiResult<Unit>
    suspend fun forgotPassword(email: String): ApiResult<Unit>
    suspend fun resetPassword(token: String, newPassword: String): ApiResult<Unit>
    suspend fun verifyEmail(token: String): ApiResult<Unit>
    fun clearTokens()
}
