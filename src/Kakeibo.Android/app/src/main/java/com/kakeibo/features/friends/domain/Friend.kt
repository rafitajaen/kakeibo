package com.kakeibo.features.friends.domain

import kotlinx.serialization.Serializable

@Serializable
data class Friend(
    val id: String,
    val username: String,
    val email: String,
    val since: String,
)
