package com.kakeibo.features.recurring.data

import com.kakeibo.features.recurring.domain.RecurringPattern
import kotlinx.serialization.Serializable
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path

interface RecurringApi {

    @GET("recurring")
    suspend fun getPatterns(): List<RecurringPattern>

    @GET("recurring/{id}")
    suspend fun getPattern(@Path("id") id: String): RecurringPattern

    @POST("recurring")
    suspend fun createPattern(@Body request: CreatePatternRequest): RecurringPattern

    @PUT("recurring/{id}")
    suspend fun updatePattern(
        @Path("id") id: String,
        @Body request: UpdatePatternRequest,
    ): RecurringPattern

    @DELETE("recurring/{id}")
    suspend fun deletePattern(@Path("id") id: String)
}

@Serializable
data class CreatePatternRequest(
    val description: String,
    val amount: Double,
    val type: String,
    val categoryId: String,
    val walletId: String,
    val frequency: String,
    val startDate: String,
)

@Serializable
data class UpdatePatternRequest(
    val description: String,
    val amount: Double,
    val frequency: String,
)
