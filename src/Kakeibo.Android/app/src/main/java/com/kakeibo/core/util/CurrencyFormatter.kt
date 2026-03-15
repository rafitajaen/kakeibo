package com.kakeibo.core.util

import java.text.NumberFormat
import java.util.Currency
import java.util.Locale
import kotlin.math.abs

/**
 * Formats monetary amounts using the JVM currency formatting system.
 */
object CurrencyFormatter {

    /**
     * Formats an amount with the given currency code.
     * Example: format(1234.56, "USD") → "$1,234.56"
     */
    fun format(amount: Double, currencyCode: String): String {
        return try {
            val formatter = NumberFormat.getCurrencyInstance()
            formatter.currency = Currency.getInstance(currencyCode)
            formatter.format(amount)
        } catch (e: Exception) {
            String.format(Locale.getDefault(), "%s %.2f", currencyCode, amount)
        }
    }

    /**
     * Formats an amount with a sign prefix.
     * Positive → "+$1,234.56", Negative → "-$1,234.56"
     */
    fun formatSigned(amount: Double, currencyCode: String): String {
        val formatted = format(abs(amount), currencyCode)
        return when {
            amount > 0 -> "+$formatted"
            amount < 0 -> "-$formatted"
            else -> formatted
        }
    }
}
