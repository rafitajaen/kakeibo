package com.kakeibo.features.friends.di

import com.kakeibo.features.friends.data.FriendsApi
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import retrofit2.Retrofit
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object FriendsModule {

    @Provides
    @Singleton
    fun provideFriendsApi(retrofit: Retrofit): FriendsApi =
        retrofit.create(FriendsApi::class.java)
}
