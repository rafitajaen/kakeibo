package com.kakeibo.features.wallets.di

import com.kakeibo.features.wallets.data.WalletsApi
import com.kakeibo.features.wallets.data.WalletsRepository
import com.kakeibo.features.wallets.data.WalletsRepositoryImpl
import dagger.Binds
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import retrofit2.Retrofit
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
abstract class WalletsModule {

    @Binds
    @Singleton
    abstract fun bindWalletsRepository(impl: WalletsRepositoryImpl): WalletsRepository

    companion object {
        @Provides
        @Singleton
        fun provideWalletsApi(retrofit: Retrofit): WalletsApi =
            retrofit.create(WalletsApi::class.java)
    }
}
