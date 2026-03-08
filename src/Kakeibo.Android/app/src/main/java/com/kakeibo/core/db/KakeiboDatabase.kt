package com.kakeibo.core.db

import androidx.room.Database
import androidx.room.RoomDatabase

/**
 * Room database for Kakeibo offline cache.
 *
 * Room is a read cache — not the source of truth.
 * Every write goes to the API; Room is populated from API responses to enable
 * instant display of stale data while a refresh is in flight.
 *
 * Add @Entity classes and @Dao interfaces to entities/daos as features are implemented.
 */
@Database(
    entities = [],          // Add entities here: WalletEntity::class, TransactionEntity::class, ...
    version = 1,
    exportSchema = true,
)
abstract class KakeiboDatabase : RoomDatabase() {
    // Add DAO abstract functions here as features are implemented:
    // abstract fun walletsDao(): WalletsDao
    // abstract fun transactionsDao(): TransactionsDao
    // abstract fun notificationsDao(): NotificationsDao
}
