package com.kakeibo.features.transactions.presentation

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
fun RecordTransactionScreen(
    preselectedWalletId: String?,
    navController: NavController,
    viewModel: TransactionsViewModel = hiltViewModel(),
) {
    val formState by viewModel.formState.collectAsStateWithLifecycle()

    var type by remember { mutableStateOf("EXPENSE") }
    var amount by remember { mutableStateOf("") }
    var description by remember { mutableStateOf("") }
    var notes by remember { mutableStateOf("") }
    var selectedWalletId by remember { mutableStateOf(preselectedWalletId ?: "") }
    var selectedWalletName by remember { mutableStateOf("") }
    var selectedCategoryId by remember { mutableStateOf("") }
    var selectedCategoryName by remember { mutableStateOf("") }
    var destinationWalletId by remember { mutableStateOf("") }
    var destinationWalletName by remember { mutableStateOf("") }
    var walletExpanded by remember { mutableStateOf(false) }
    var categoryExpanded by remember { mutableStateOf(false) }
    var destWalletExpanded by remember { mutableStateOf(false) }

    val today = Clock.System.todayIn(TimeZone.currentSystemDefault()).toString()

    LaunchedEffect(Unit) {
        viewModel.loadFormData()
        viewModel.events.collectLatest { event ->
            if (event is TransactionEvent.Saved) navController.popBackStack()
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Record Transaction") },
                navigationIcon = {
                    IconButton(onClick = { navController.popBackStack() }) {
                        Icon(Icons.Filled.ArrowBack, contentDescription = "Back")
                    }
                },
            )
        },
    ) { padding ->
        when (val state = formState) {
            is RecordTransactionUiState.Loading -> LoadingState(modifier = Modifier.padding(padding))
            is RecordTransactionUiState.Error -> ErrorState(
                message = state.message,
                onRetry = viewModel::loadFormData,
                modifier = Modifier.padding(padding),
            )
            else -> {
                val wallets = if (state is RecordTransactionUiState.Ready) state.wallets else emptyList()
                val categories = if (state is RecordTransactionUiState.Ready) state.categories else emptyList()

                // Pre-fill wallet name when wallet id is preselected
                LaunchedEffect(wallets) {
                    if (preselectedWalletId != null && selectedWalletName.isBlank()) {
                        wallets.find { it.id == preselectedWalletId }?.let {
                            selectedWalletName = it.name
                        }
                    }
                }

                Column(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(padding)
                        .padding(horizontal = 24.dp)
                        .verticalScroll(rememberScrollState()),
                ) {
                    Spacer(modifier = Modifier.height(16.dp))

                    // Type selector
                    Text(
                        text = "Type",
                        style = MaterialTheme.typography.labelLarge,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                    Spacer(modifier = Modifier.height(8.dp))
                    Row(horizontalArrangement = androidx.compose.foundation.layout.Arrangement.spacedBy(8.dp)) {
                        listOf("INCOME", "EXPENSE", "TRANSFER").forEach { t ->
                            FilterChip(
                                selected = type == t,
                                onClick = { type = t },
                                label = { Text(t.lowercase().replaceFirstChar { it.uppercase() }) },
                            )
                        }
                    }
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

                    OutlinedTextField(
                        value = description,
                        onValueChange = { description = it },
                        label = { Text("Description") },
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth(),
                    )
                    Spacer(modifier = Modifier.height(16.dp))

                    // Wallet selector
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

                    // Category selector
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

                    // Destination wallet for TRANSFER
                    if (type == "TRANSFER") {
                        Spacer(modifier = Modifier.height(16.dp))
                        ExposedDropdownMenuBox(expanded = destWalletExpanded, onExpandedChange = { destWalletExpanded = it }) {
                            OutlinedTextField(
                                value = destinationWalletName.ifBlank { "Select destination wallet" },
                                onValueChange = {},
                                readOnly = true,
                                label = { Text("Destination Wallet") },
                                trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = destWalletExpanded) },
                                modifier = Modifier.menuAnchor(MenuAnchorType.PrimaryNotEditable).fillMaxWidth(),
                            )
                            ExposedDropdownMenu(expanded = destWalletExpanded, onDismissRequest = { destWalletExpanded = false }) {
                                wallets.filter { it.id != selectedWalletId }.forEach { wallet ->
                                    DropdownMenuItem(
                                        text = { Text(wallet.name) },
                                        onClick = {
                                            destinationWalletId = wallet.id
                                            destinationWalletName = wallet.name
                                            destWalletExpanded = false
                                        },
                                    )
                                }
                            }
                        }
                    }

                    Spacer(modifier = Modifier.height(16.dp))
                    OutlinedTextField(
                        value = notes,
                        onValueChange = { notes = it },
                        label = { Text("Notes (optional)") },
                        modifier = Modifier.fillMaxWidth(),
                        maxLines = 3,
                    )

                    Spacer(modifier = Modifier.height(32.dp))
                    val canSubmit = amount.isNotBlank() && description.isNotBlank() &&
                        selectedWalletId.isNotBlank() && selectedCategoryId.isNotBlank() &&
                        (type != "TRANSFER" || destinationWalletId.isNotBlank())
                    Button(
                        onClick = {
                            val amt = amount.toDoubleOrNull() ?: return@Button
                            viewModel.createTransaction(
                                walletId = selectedWalletId,
                                type = type,
                                amount = amt,
                                description = description,
                                categoryId = selectedCategoryId,
                                date = today,
                                notes = notes,
                                destinationWalletId = if (type == "TRANSFER") destinationWalletId else null,
                            )
                        },
                        enabled = canSubmit,
                        modifier = Modifier.fillMaxWidth().height(52.dp),
                    ) { Text("Save Transaction") }
                    Spacer(modifier = Modifier.height(24.dp))
                }
            }
        }
    }
}
