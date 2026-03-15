package com.kakeibo.features.activity.domain

import kotlinx.serialization.Serializable

@Serializable
data class ActivityEntry(
    val id: String,
    val action: String,
    val entityType: String,
    val entityId: String?,
    val description: String,
    val occurredAt: String,
)
