package com.kakeibo.features.budgets.data

import com.kakeibo.features.budgets.domain.Budget
import kotlinx.serialization.Serializable
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path

interface BudgetsApi {

    @GET("budgets")
    suspend fun getBudgets(): List<Budget>

    @GET("budgets/{id}")
    suspend fun getBudget(@Path("id") id: String): Budget

    @POST("budgets")
    suspend fun createBudget(@Body request: CreateBudgetRequest): Budget

    @PUT("budgets/{id}")
    suspend fun updateBudget(@Path("id") id: String, @Body request: UpdateBudgetRequest): Budget

    @DELETE("budgets/{id}")
    suspend fun deleteBudget(@Path("id") id: String)
}

@Serializable
data class CreateBudgetRequest(
    val name: String,
    val categoryId: String,
    val limitAmount: Double,
    val periodStart: String,
    val periodEnd: String,
)

@Serializable
data class UpdateBudgetRequest(
    val name: String,
    val limitAmount: Double,
)
