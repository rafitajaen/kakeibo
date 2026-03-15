package com.kakeibo.features.goals.data

import com.kakeibo.features.goals.domain.Goal
import kotlinx.serialization.Serializable
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path

interface GoalsApi {

    @GET("goals")
    suspend fun getGoals(): List<Goal>

    @GET("goals/{id}")
    suspend fun getGoal(@Path("id") id: String): Goal

    @POST("goals")
    suspend fun createGoal(@Body request: CreateGoalRequest): Goal

    @PUT("goals/{id}")
    suspend fun updateGoal(@Path("id") id: String, @Body request: UpdateGoalRequest): Goal

    @DELETE("goals/{id}")
    suspend fun deleteGoal(@Path("id") id: String)
}

@Serializable
data class CreateGoalRequest(
    val name: String,
    val targetAmount: Double,
    val deadline: String? = null,
    val linkedWalletId: String? = null,
)

@Serializable
data class UpdateGoalRequest(
    val name: String,
    val targetAmount: Double,
    val deadline: String? = null,
)
