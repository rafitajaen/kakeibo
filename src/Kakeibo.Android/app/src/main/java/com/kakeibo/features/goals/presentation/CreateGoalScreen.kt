package com.kakeibo.features.goals.presentation

import androidx.compose.foundation.layout.Column
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
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
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

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CreateGoalScreen(
    navController: NavController,
    viewModel: GoalsViewModel = hiltViewModel(),
) {
    val formState by viewModel.createState.collectAsStateWithLifecycle()

    var name by remember { mutableStateOf("") }
    var targetAmount by remember { mutableStateOf("") }
    var deadline by remember { mutableStateOf("") }
    var selectedWalletId by remember { mutableStateOf<String?>(null) }
    var selectedWalletName by remember { mutableStateOf("None (manual)") }
    var walletExpanded by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) {
        viewModel.loadCreateForm()
        viewModel.events.collectLatest { event ->
            if (event is GoalEvent.Saved) navController.popBackStack()
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("New Goal") },
                navigationIcon = {
                    IconButton(onClick = { navController.popBackStack() }) {
                        Icon(Icons.Filled.ArrowBack, contentDescription = "Back")
                    }
                },
            )
        },
    ) { padding ->
        when (val state = formState) {
            is CreateGoalUiState.Loading -> LoadingState(modifier = Modifier.padding(padding))
            is CreateGoalUiState.Error -> ErrorState(
                message = state.message,
                onRetry = viewModel::loadCreateForm,
                modifier = Modifier.padding(padding),
            )
            else -> {
                val wallets = if (state is CreateGoalUiState.Ready) state.wallets else emptyList()
                Column(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(padding)
                        .padding(horizontal = 24.dp)
                        .verticalScroll(rememberScrollState()),
                ) {
                    Spacer(modifier = Modifier.height(16.dp))
                    OutlinedTextField(
                        value = name,
                        onValueChange = { name = it },
                        label = { Text("Goal Name") },
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth(),
                    )
                    Spacer(modifier = Modifier.height(16.dp))
                    OutlinedTextField(
                        value = targetAmount,
                        onValueChange = { targetAmount = it },
                        label = { Text("Target Amount") },
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth(),
                    )
                    Spacer(modifier = Modifier.height(16.dp))
                    OutlinedTextField(
                        value = deadline,
                        onValueChange = { deadline = it },
                        label = { Text("Deadline (optional, YYYY-MM-DD)") },
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth(),
                    )
                    Spacer(modifier = Modifier.height(16.dp))
                    ExposedDropdownMenuBox(
                        expanded = walletExpanded,
                        onExpandedChange = { walletExpanded = it },
                    ) {
                        OutlinedTextField(
                            value = selectedWalletName,
                            onValueChange = {},
                            readOnly = true,
                            label = { Text("Linked Wallet (optional)") },
                            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = walletExpanded) },
                            modifier = Modifier
                                .menuAnchor(MenuAnchorType.PrimaryNotEditable)
                                .fillMaxWidth(),
                        )
                        ExposedDropdownMenu(
                            expanded = walletExpanded,
                            onDismissRequest = { walletExpanded = false },
                        ) {
                            DropdownMenuItem(
                                text = { Text("None (manual)") },
                                onClick = {
                                    selectedWalletId = null
                                    selectedWalletName = "None (manual)"
                                    walletExpanded = false
                                },
                            )
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
                    Spacer(modifier = Modifier.height(32.dp))
                    Button(
                        onClick = {
                            val amount = targetAmount.toDoubleOrNull() ?: return@Button
                            viewModel.createGoal(
                                name = name,
                                targetAmount = amount,
                                deadline = deadline.takeIf { it.isNotBlank() },
                                linkedWalletId = selectedWalletId,
                            )
                        },
                        enabled = name.isNotBlank() && targetAmount.isNotBlank(),
                        modifier = Modifier.fillMaxWidth().height(52.dp),
                    ) { Text("Create Goal") }
                    Spacer(modifier = Modifier.height(24.dp))
                }
            }
        }
    }
}
