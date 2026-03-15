package com.kakeibo.features.recurring.di

import com.kakeibo.features.recurring.data.RecurringApi
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import retrofit2.Retrofit
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object RecurringModule {

    @Provides
    @Singleton
    fun provideRecurringApi(retrofit: Retrofit): RecurringApi =
        retrofit.create(RecurringApi::class.java)
}
