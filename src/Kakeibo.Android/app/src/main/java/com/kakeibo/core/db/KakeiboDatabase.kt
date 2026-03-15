package com.kakeibo.core.db

import androidx.room.Database
import androidx.room.RoomDatabase
import com.kakeibo.features.transactions.data.db.TransactionEntity
import com.kakeibo.features.transactions.data.db.TransactionsDao
import com.kakeibo.features.wallets.data.db.WalletEntity
import com.kakeibo.features.wallets.data.db.WalletsDao

/**
 * Room database for Kakeibo offline cache.
 *
 * Room is a read cache — not the source of truth.
 * Every write goes to the API; Room is populated from API responses to enable
 * instant display of stale data while a refresh is in flight.
 */
@Database(
    entities = [WalletEntity::class, TransactionEntity::class],
    version = 2,
    exportSchema = false,
)
abstract class KakeiboDatabase : RoomDatabase() {
    abstract fun walletsDao(): WalletsDao
    abstract fun transactionsDao(): TransactionsDao
}
