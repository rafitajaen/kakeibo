package com.kakeibo.features.goals.di

import com.kakeibo.features.goals.data.GoalsApi
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import retrofit2.Retrofit
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object GoalsModule {

    @Provides
    @Singleton
    fun provideGoalsApi(retrofit: Retrofit): GoalsApi =
        retrofit.create(GoalsApi::class.java)
}
