package com.kakeibo.features.notifications.di

import com.kakeibo.features.notifications.data.NotificationsApi
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import retrofit2.Retrofit
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object NotificationsModule {

    @Provides
    @Singleton
    fun provideNotificationsApi(retrofit: Retrofit): NotificationsApi =
        retrofit.create(NotificationsApi::class.java)
}
