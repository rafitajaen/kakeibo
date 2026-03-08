package com.kakeibo.core.di

import android.content.Context
import androidx.room.Room
import com.kakeibo.core.db.KakeiboDatabase
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.android.qualifiers.ApplicationContext
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object DatabaseModule {

    @Provides
    @Singleton
    fun provideDatabase(@ApplicationContext context: Context): KakeiboDatabase =
        Room.databaseBuilder(
            context,
            KakeiboDatabase::class.java,
            "kakeibo.db",
        ).build()
}
