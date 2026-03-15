package com.kakeibo.features.recurring.presentation

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material3.Button
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.MenuAnchorType
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavController
import com.kakeibo.core.ui.ErrorState
import com.kakeibo.core.ui.LoadingState
import kotlinx.coroutines.flow.collectLatest
import kotlinx.datetime.Clock
import kotlinx.datetime.TimeZone
import kotlinx.datetime.todayIn

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CreatePatternScreen(
    navController: NavController,
    viewModel: RecurringViewModel = hiltViewModel(),
) {
    val formState by viewModel.createState.collectAsStateWithLifecycle()

    var description by remember { mutableStateOf("") }
    var amount by remember { mutableStateOf("") }
    var type by remember { mutableStateOf("EXPENSE") }
    var frequency by remember { mutableStateOf("MONTHLY") }
    var selectedWalletId by remember { mutableStateOf("") }
    var selectedWalletName by remember { mutableStateOf("") }
    var selectedCategoryId by remember { mutableStateOf("") }
    var selectedCategoryName by remember { mutableStateOf("") }
    var walletExpanded by remember { mutableStateOf(false) }
    var categoryExpanded by remember { mutableStateOf(false) }

    val today = Clock.System.todayIn(TimeZone.currentSystemDefault()).toString()

    LaunchedEffect(Unit) {
        viewModel.loadCreateForm()
        viewModel.events.collectLatest { event ->
            if (event is RecurringEvent.Saved) navController.popBackStack()
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("New Recurring Pattern") },
                navigationIcon = {
                    IconButton(onClick = { navController.popBackStack() }) {
                        Icon(Icons.Filled.ArrowBack, contentDescription = "Back")
                    }
                },
            )
        },
    ) { padding ->
        when (val state = formState) {
            is CreatePatternUiState.Loading -> LoadingState(modifier = Modifier.padding(padding))
            is CreatePatternUiState.Error -> ErrorState(
                message = state.message,
                onRetry = viewModel::loadCreateForm,
                modifier = Modifier.padding(padding),
            )
            else -> {
                val wallets = if (state is CreatePatternUiState.Ready) state.wallets else emptyList()
                val categories = if (state is CreatePatternUiState.Ready) state.categories else emptyList()
                Column(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(padding)
                        .padding(horizontal = 24.dp)
                        .verticalScroll(rememberScrollState()),
                ) {
                    Spacer(modifier = Modifier.height(16.dp))
                    OutlinedTextField(
                        value = description,
                        onValueChange = { description = it },
                        label = { Text("Description") },
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth(),
                    )
                    Spacer(modifier = Modifier.height(16.dp))
                    OutlinedTextField(
                        value = amount,
                        onValueChange = { amount = it },
                        label = { Text("Amount") },
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth(),
                    )
                    Spacer(modifier = Modifier.height(16.dp))
                    Text(
                        text = "Type",
                        style = MaterialTheme.typography.labelLarge,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                    Spacer(modifier = Modifier.height(8.dp))
                    Row(horizontalArrangement = androidx.compose.foundation.layout.Arrangement.spacedBy(8.dp)) {
                        listOf("INCOME", "EXPENSE").forEach { t ->
                            FilterChip(
                                selected = type == t,
                                onClick = { type = t },
                                label = { Text(t.lowercase().replaceFirstChar { it.uppercase() }) },
                            )
                        }
                    }
                    Spacer(modifier = Modifier.height(16.dp))
                    Text(
                        text = "Frequency",
                        style = MaterialTheme.typography.labelLarge,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                    Spacer(modifier = Modifier.height(8.dp))
                    Row(horizontalArrangement = androidx.compose.foundation.layout.Arrangement.spacedBy(8.dp)) {
                        listOf("DAILY", "WEEKLY", "MONTHLY", "YEARLY").forEach { f ->
                            FilterChip(
                                selected = frequency == f,
                                onClick = { frequency = f },
                                label = { Text(f.lowercase().replaceFirstChar { it.uppercase() }) },
                            )
                        }
                    }
                    Spacer(modifier = Modifier.height(16.dp))
                    ExposedDropdownMenuBox(expanded = walletExpanded, onExpandedChange = { walletExpanded = it }) {
                        OutlinedTextField(
                            value = selectedWalletName.ifBlank { "Select wallet" },
                            onValueChange = {},
                            readOnly = true,
                            label = { Text("Wallet") },
                            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = walletExpanded) },
                            modifier = Modifier.menuAnchor(MenuAnchorType.PrimaryNotEditable).fillMaxWidth(),
                        )
                        ExposedDropdownMenu(expanded = walletExpanded, onDismissRequest = { walletExpanded = false }) {
                            wallets.forEach { wallet ->
                                DropdownMenuItem(
                                    text = { Text(wallet.name) },
                                    onClick = {
                                        selectedWalletId = wallet.id
                                        selectedWalletName = wallet.name
                                        walletExpanded = false
                                    },
                                )
                            }
                        }
                    }
                    Spacer(modifier = Modifier.height(16.dp))
                    ExposedDropdownMenuBox(expanded = categoryExpanded, onExpandedChange = { categoryExpanded = it }) {
                        OutlinedTextField(
                            value = selectedCategoryName.ifBlank { "Select category" },
                            onValueChange = {},
                            readOnly = true,
                            label = { Text("Category") },
                            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = categoryExpanded) },
                            modifier = Modifier.menuAnchor(MenuAnchorType.PrimaryNotEditable).fillMaxWidth(),
                        )
                        ExposedDropdownMenu(expanded = categoryExpanded, onDismissRequest = { categoryExpanded = false }) {
                            categories.forEach { cat ->
                                DropdownMenuItem(
                                    text = { Text(cat.name) },
                                    onClick = {
                                        selectedCategoryId = cat.id
                                        selectedCategoryName = cat.name
                                        categoryExpanded = false
                                    },
                                )
                            }
                        }
                    }
                    Spacer(modifier = Modifier.height(32.dp))
                    Button(
                        onClick = {
                            val amt = amount.toDoubleOrNull() ?: return@Button
                            viewModel.createPattern(description, amt, type, selectedCategoryId, selectedWalletId, frequency, today)
                        },
                        enabled = description.isNotBlank() && amount.isNotBlank() && selectedWalletId.isNotBlank() && selectedCategoryId.isNotBlank(),
                        modifier = Modifier.fillMaxWidth().height(52.dp),
                    ) { Text("Create Pattern") }
                    Spacer(modifier = Modifier.height(24.dp))
                }
            }
        }
    }
}
