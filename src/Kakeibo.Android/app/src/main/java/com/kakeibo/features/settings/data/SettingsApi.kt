package com.kakeibo.features.settings.data

import com.kakeibo.features.auth.domain.User
import kotlinx.serialization.Serializable
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path

interface SettingsApi {

    @GET("auth/me")
    suspend fun getProfile(): User

    @PUT("users/me")
    suspend fun updateProfile(@Body request: UpdateProfileRequest): User

    @PUT("users/me/password")
    suspend fun changePassword(@Body request: ChangePasswordRequest)

    @DELETE("users/me")
    suspend fun deleteAccount()

    @POST("auth/logout")
    suspend fun logout()
}

@Serializable
data class UpdateProfileRequest(val username: String)

@Serializable
data class ChangePasswordRequest(
    val currentPassword: String,
    val newPassword: String,
)
