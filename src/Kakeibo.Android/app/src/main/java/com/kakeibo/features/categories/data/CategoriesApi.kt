package com.kakeibo.features.categories.data

import com.kakeibo.features.categories.domain.Category
import kotlinx.serialization.Serializable
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path

interface CategoriesApi {

    @GET("categories")
    suspend fun getCategories(): List<Category>

    @POST("categories")
    suspend fun createCategory(@Body request: CreateCategoryRequest): Category

    @PUT("categories/{id}")
    suspend fun updateCategory(@Path("id") id: String, @Body request: UpdateCategoryRequest): Category

    @DELETE("categories/{id}")
    suspend fun deleteCategory(@Path("id") id: String)
}

@Serializable
data class CreateCategoryRequest(
    val name: String,
    val color: String? = null,
    val isPrivate: Boolean = false,
)

@Serializable
data class UpdateCategoryRequest(
    val name: String,
    val color: String? = null,
    val isPrivate: Boolean = false,
)
