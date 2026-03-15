package com.kakeibo.features.wallets.data.db

import androidx.room.Dao
import androidx.room.Query
import androidx.room.Upsert
import kotlinx.coroutines.flow.Flow

@Dao
interface WalletsDao {

    @Query("SELECT * FROM wallets WHERE isArchived = 0 ORDER BY name ASC")
    fun observeActiveWallets(): Flow<List<WalletEntity>>

    @Upsert
    suspend fun upsertAll(wallets: List<WalletEntity>)

    @Query("DELETE FROM wallets")
    suspend fun deleteAll()
}
