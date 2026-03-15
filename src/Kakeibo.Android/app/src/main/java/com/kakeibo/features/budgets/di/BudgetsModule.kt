package com.kakeibo.features.budgets.di

import com.kakeibo.features.budgets.data.BudgetsApi
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import retrofit2.Retrofit
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object BudgetsModule {

    @Provides
    @Singleton
    fun provideBudgetsApi(retrofit: Retrofit): BudgetsApi =
        retrofit.create(BudgetsApi::class.java)
}
