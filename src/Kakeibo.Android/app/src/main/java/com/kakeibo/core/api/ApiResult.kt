package com.kakeibo.core.api

import retrofit2.HttpException
import java.io.IOException

sealed interface ApiResult<out T> {
    data class Success<T>(val data: T) : ApiResult<T>
    data class Error(val code: Int, val message: String) : ApiResult<Nothing>
    data object NetworkError : ApiResult<Nothing>
}

suspend fun <T> safeApiCall(call: suspend () -> T): ApiResult<T> {
    return try {
        ApiResult.Success(call())
    } catch (e: HttpException) {
        val errorBody = e.response()?.errorBody()?.string() ?: e.message()
        ApiResult.Error(e.code(), errorBody ?: "Unknown error")
    } catch (e: IOException) {
        ApiResult.NetworkError
    }
}
