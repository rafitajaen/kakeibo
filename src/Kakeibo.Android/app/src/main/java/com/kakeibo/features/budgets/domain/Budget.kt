package com.kakeibo.features.budgets.domain

import kotlinx.serialization.Serializable

@Serializable
data class Budget(
    val id: String,
    val name: String,
    val categoryId: String,
    val categoryName: String,
    val limitAmount: Double,
    val currentSpending: Double,
    val currency: String,
    val periodStart: String,
    val periodEnd: String,
    val status: BudgetStatus,
    val percentUsed: Double,
)

@Serializable
enum class BudgetStatus { ON_TRACK, WARNING, EXCEEDED }
