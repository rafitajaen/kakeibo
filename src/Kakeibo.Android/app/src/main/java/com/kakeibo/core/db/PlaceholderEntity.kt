package com.kakeibo.core.db

import androidx.room.Entity
import androidx.room.PrimaryKey

/**
 * Placeholder entity to satisfy Room's requirement of at least one entity.
 * Remove when real entities (WalletEntity, TransactionEntity, etc.) are added.
 */
@Entity(tableName = "_placeholder")
internal data class PlaceholderEntity(
    @PrimaryKey val id: Int = 0,
)
