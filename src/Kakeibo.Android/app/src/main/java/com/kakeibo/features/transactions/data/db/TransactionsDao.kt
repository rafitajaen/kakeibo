package com.kakeibo.features.transactions.data.db

import androidx.room.Dao
import androidx.room.Query
import androidx.room.Upsert
import kotlinx.coroutines.flow.Flow

@Dao
interface TransactionsDao {

    @Query("SELECT * FROM transactions WHERE walletId = :walletId ORDER BY date DESC")
    fun observeByWallet(walletId: String): Flow<List<TransactionEntity>>

    @Upsert
    suspend fun upsertAll(transactions: List<TransactionEntity>)

    @Query("DELETE FROM transactions WHERE walletId = :walletId")
    suspend fun deleteByWallet(walletId: String)

    @Query("DELETE FROM transactions")
    suspend fun deleteAll()
}
