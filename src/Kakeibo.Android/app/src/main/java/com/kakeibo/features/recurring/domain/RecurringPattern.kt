package com.kakeibo.features.recurring.domain

import kotlinx.serialization.Serializable

@Serializable
data class RecurringPattern(
    val id: String,
    val description: String,
    val amount: Double,
    val type: String,
    val categoryId: String,
    val categoryName: String,
    val walletId: String,
    val walletName: String,
    val frequency: String,
    val nextOccurrence: String?,
    val isActive: Boolean,
)
