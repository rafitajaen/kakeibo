package com.kakeibo.features.transactions.di

import com.kakeibo.features.transactions.data.TransactionsApi
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import retrofit2.Retrofit
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object TransactionsModule {

    @Provides
    @Singleton
    fun provideTransactionsApi(retrofit: Retrofit): TransactionsApi =
        retrofit.create(TransactionsApi::class.java)
}
