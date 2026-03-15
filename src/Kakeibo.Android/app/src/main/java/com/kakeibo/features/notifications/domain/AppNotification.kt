package com.kakeibo.features.notifications.domain

import kotlinx.serialization.Serializable

@Serializable
data class AppNotification(
    val id: String,
    val title: String,
    val body: String,
    val type: String,
    val isRead: Boolean,
    val createdAt: String,
)
