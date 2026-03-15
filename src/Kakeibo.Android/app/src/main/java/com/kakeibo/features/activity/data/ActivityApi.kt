package com.kakeibo.features.activity.data

import com.kakeibo.features.activity.domain.ActivityEntry
import retrofit2.http.GET
import retrofit2.http.Query

interface ActivityApi {

    @GET("activity")
    suspend fun getActivityFeed(
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 50,
    ): List<ActivityEntry>
}
