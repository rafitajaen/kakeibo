package com.kakeibo.features.activity.di

import com.kakeibo.features.activity.data.ActivityApi
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import retrofit2.Retrofit
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object ActivityModule {

    @Provides
    @Singleton
    fun provideActivityApi(retrofit: Retrofit): ActivityApi =
        retrofit.create(ActivityApi::class.java)
}
