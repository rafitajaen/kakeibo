package com.kakeibo.features.categories.di

import com.kakeibo.features.categories.data.CategoriesApi
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import retrofit2.Retrofit
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object CategoriesModule {

    @Provides
    @Singleton
    fun provideCategoriesApi(retrofit: Retrofit): CategoriesApi =
        retrofit.create(CategoriesApi::class.java)
}
