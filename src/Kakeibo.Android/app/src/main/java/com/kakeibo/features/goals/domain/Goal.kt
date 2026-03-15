package com.kakeibo.features.goals.domain

import kotlinx.serialization.Serializable

@Serializable
data class Goal(
    val id: String,
    val name: String,
    val targetAmount: Double,
    val currentAmount: Double,
    val currency: String,
    val deadline: String? = null,
    val isAchieved: Boolean,
    val percentComplete: Double,
    val linkedWalletId: String? = null,
    val linkedWalletName: String? = null,
)
