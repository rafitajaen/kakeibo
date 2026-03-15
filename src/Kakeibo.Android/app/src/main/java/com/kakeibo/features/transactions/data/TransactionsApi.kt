package com.kakeibo.features.transactions.data

import com.kakeibo.features.dashboard.domain.DashboardSummary
import com.kakeibo.features.transactions.domain.Transaction
import kotlinx.serialization.Serializable
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

interface TransactionsApi {

    @GET("transactions/dashboard")
    suspend fun getDashboardSummary(): DashboardSummary

    @GET("transactions")
    suspend fun getTransactions(
        @Query("walletId") walletId: String? = null,
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 50,
    ): TransactionListResponse

    @GET("transactions/all")
    suspend fun getAllTransactions(
        @Query("from") from: String? = null,
        @Query("to") to: String? = null,
    ): List<Transaction>

    @GET("transactions/{id}")
    suspend fun getTransaction(@Path("id") id: String): Transaction

    @POST("transactions")
    suspend fun createTransaction(@Body request: CreateTransactionRequest): Transaction

    @PUT("transactions/{id}")
    suspend fun updateTransaction(
        @Path("id") id: String,
        @Body request: UpdateTransactionRequest,
    ): Transaction

    @DELETE("transactions/{id}")
    suspend fun deleteTransaction(@Path("id") id: String)
}

@Serializable
data class TransactionListResponse(
    val items: List<Transaction>,
    val total: Int,
    val page: Int,
    val pageSize: Int,
)

@Serializable
data class CreateTransactionRequest(
    val walletId: String,
    val type: String,
    val amount: Double,
    val description: String,
    val categoryId: String,
    val date: String,
    val notes: String = "",
    val destinationWalletId: String? = null,
)

@Serializable
data class UpdateTransactionRequest(
    val amount: Double,
    val description: String,
    val categoryId: String,
    val date: String,
    val notes: String = "",
)
