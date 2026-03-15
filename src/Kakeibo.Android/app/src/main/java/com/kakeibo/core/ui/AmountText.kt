package com.kakeibo.core.ui

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight

enum class AmountType { INCOME, EXPENSE, TRANSFER }

/**
 * Text that colors the amount according to its transaction type.
 * Income → tertiary (green), Expense → error (red), Transfer → secondary.
 */
@Composable
fun AmountText(
    amount: String,
    type: AmountType,
    modifier: Modifier = Modifier,
    style: TextStyle = MaterialTheme.typography.bodyLarge,
    fontWeight: FontWeight = FontWeight.SemiBold,
) {
    val color = when (type) {
        AmountType.INCOME -> MaterialTheme.colorScheme.tertiary
        AmountType.EXPENSE -> MaterialTheme.colorScheme.error
        AmountType.TRANSFER -> MaterialTheme.colorScheme.secondary
    }
    Text(
        text = amount,
        color = color,
        style = style,
        fontWeight = fontWeight,
        modifier = modifier,
    )
}
