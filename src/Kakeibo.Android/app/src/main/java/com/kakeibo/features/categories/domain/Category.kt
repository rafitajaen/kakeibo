package com.kakeibo.features.categories.domain

import kotlinx.serialization.Serializable

@Serializable
data class Category(
    val id: String,
    val name: String,
    val isSystem: Boolean,
    val color: String?,
    val isPrivate: Boolean = false,
)
