package com.kakeibo.features.friends.data

import com.kakeibo.features.friends.domain.Friend
import kotlinx.serialization.Serializable
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Path

interface FriendsApi {

    @GET("friends")
    suspend fun getFriends(): List<Friend>

    @POST("friends/request")
    suspend fun sendRequest(@Body request: FriendRequestBody)

    @DELETE("friends/{id}")
    suspend fun removeFriend(@Path("id") id: String)
}

@Serializable
data class FriendRequestBody(val email: String)
