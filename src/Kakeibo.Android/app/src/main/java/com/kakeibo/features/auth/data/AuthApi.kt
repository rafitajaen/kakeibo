package com.kakeibo.features.auth.data

import com.kakeibo.features.auth.domain.User
import kotlinx.serialization.Serializable
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST

interface AuthApi {

    @POST("auth/login")
    suspend fun login(@Body request: LoginRequest): TokenResponse

    @POST("auth/register")
    suspend fun register(@Body request: RegisterRequest): TokenResponse

    @GET("auth/me")
    suspend fun me(): User

    @POST("auth/logout")
    suspend fun logout()

    @POST("auth/refresh")
    suspend fun refresh(@Body request: RefreshRequest): TokenResponse

    @POST("auth/forgot-password")
    suspend fun forgotPassword(@Body request: ForgotPasswordRequest)

    @POST("auth/reset-password")
    suspend fun resetPassword(@Body request: ResetPasswordRequest)

    @POST("auth/verify-email")
    suspend fun verifyEmail(@Body request: VerifyEmailRequest)
}

@Serializable data class LoginRequest(val email: String, val password: String)

@Serializable data class RegisterRequest(
    val email: String,
    val password: String,
    val currency: String,
)

@Serializable data class RefreshRequest(val refreshToken: String)

@Serializable data class ForgotPasswordRequest(val email: String)

@Serializable data class ResetPasswordRequest(val token: String, val newPassword: String)

@Serializable data class VerifyEmailRequest(val token: String)

@Serializable data class TokenResponse(val accessToken: String, val refreshToken: String)
