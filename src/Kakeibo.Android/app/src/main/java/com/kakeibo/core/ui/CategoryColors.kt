package com.kakeibo.core.ui

import androidx.compose.ui.graphics.Color

/**
 * OneMoney-inspired category colors for the 12 system categories.
 * Index order matches the platform's system category list (1-based).
 */
object CategoryColors {

    val Housing = Color(0xFF5B8FF9)
    val Transportation = Color(0xFF5AD8A6)
    val FoodDining = Color(0xFFFF6B6B)
    val HealthWellness = Color(0xFFFF9D4D)
    val Entertainment = Color(0xFFAD65FF)
    val Shopping = Color(0xFFFF85C2)
    val Education = Color(0xFF6DC8EC)
    val Subscriptions = Color(0xFF95DE64)
    val SavingsInvestments = Color(0xFF3AA272)
    val DebtLoans = Color(0xFFFF4D4F)
    val GiftsDonations = Color(0xFFF7C739)
    val Other = Color(0xFFBFBFBF)

    private val all = listOf(
        Housing,
        Transportation,
        FoodDining,
        HealthWellness,
        Entertainment,
        Shopping,
        Education,
        Subscriptions,
        SavingsInvestments,
        DebtLoans,
        GiftsDonations,
        Other,
    )

    /** Returns a color for a 1-based system category index. */
    fun forSystemIndex(index: Int): Color = all.getOrElse(index - 1) { Other }

    /** Returns a deterministic color for any category name. */
    fun forName(name: String): Color {
        val index = Math.floorMod(name.hashCode(), all.size)
        return all[index]
    }

    /** Returns a color from a hex string like "#FF5B8FF9", falling back to [Other]. */
    fun fromHex(hex: String?): Color {
        if (hex == null) return Other
        return try {
            val cleaned = hex.trimStart('#')
            val value = cleaned.toLong(16)
            when (cleaned.length) {
                6 -> Color(0xFF000000L or value)
                8 -> Color(value)
                else -> Other
            }
        } catch (e: NumberFormatException) {
            Other
        }
    }
}
