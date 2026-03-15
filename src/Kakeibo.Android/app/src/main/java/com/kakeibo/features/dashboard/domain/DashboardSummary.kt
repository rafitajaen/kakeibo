package com.kakeibo.features.dashboard.domain

import kotlinx.serialization.Serializable

@Serializable
data class DashboardSummary(
    val totalIncome: Double,
    val totalExpenses: Double,
    val netSavings: Double,
    val currency: String,
    val recentTransactions: List<RecentTransaction>,
)

@Serializable
data class RecentTransaction(
    val id: String,
    val description: String,
    val amount: Double,
    val type: String,
    val categoryName: String,
    val categoryColor: String?,
    val walletName: String,
    val date: String,
)
