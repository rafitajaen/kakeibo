package com.kakeibo.features.notifications.data

import com.kakeibo.features.notifications.domain.AppNotification
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Path
import retrofit2.http.Query

interface NotificationsApi {

    @GET("notifications")
    suspend fun getNotifications(
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 50,
    ): List<AppNotification>

    @POST("notifications/{id}/read")
    suspend fun markAsRead(@Path("id") id: String)

    @POST("notifications/read-all")
    suspend fun markAllAsRead()
}
