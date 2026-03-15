package com.kakeibo.features.settings.di

import com.kakeibo.features.settings.data.SettingsApi
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import retrofit2.Retrofit
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object SettingsModule {

    @Provides
    @Singleton
    fun provideSettingsApi(retrofit: Retrofit): SettingsApi =
        retrofit.create(SettingsApi::class.java)
}
