package com.kakeibo.features.budgets.presentation

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material3.Button
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
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
fun EditBudgetScreen(
    budgetId: String,
    navController: NavController,
    viewModel: BudgetsViewModel = hiltViewModel(),
) {
    val uiState by viewModel.detailState.collectAsStateWithLifecycle()

    var name by remember { mutableStateOf("") }
    var limitAmount by remember { mutableStateOf("") }
    var initialized by remember { mutableStateOf(false) }

    LaunchedEffect(budgetId) { viewModel.loadDetail(budgetId) }
    LaunchedEffect(uiState) {
        if (!initialized && uiState is BudgetDetailUiState.Success) {
            val budget = (uiState as BudgetDetailUiState.Success).budget
            name = budget.name
            limitAmount = budget.limitAmount.toString()
            initialized = true
        }
    }
    LaunchedEffect(Unit) {
        viewModel.events.collectLatest { event ->
            if (event is BudgetEvent.Saved) navController.popBackStack()
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Edit Budget") },
                navigationIcon = {
                    IconButton(onClick = { navController.popBackStack() }) {
                        Icon(Icons.Filled.ArrowBack, contentDescription = "Back")
                    }
                },
            )
        },
    ) { padding ->
        when (val state = uiState) {
            is BudgetDetailUiState.Loading -> LoadingState(modifier = Modifier.padding(padding))
            is BudgetDetailUiState.Error -> ErrorState(
                message = state.message,
                onRetry = { viewModel.loadDetail(budgetId) },
                modifier = Modifier.padding(padding),
            )
            is BudgetDetailUiState.Success -> Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding)
                    .padding(horizontal = 24.dp),
            ) {
                Spacer(modifier = Modifier.height(16.dp))
                OutlinedTextField(
                    value = name,
                    onValueChange = { name = it },
                    label = { Text("Budget Name") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth(),
                )
                Spacer(modifier = Modifier.height(16.dp))
                OutlinedTextField(
                    value = limitAmount,
                    onValueChange = { limitAmount = it },
                    label = { Text("Limit Amount") },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth(),
                )
                Spacer(modifier = Modifier.height(32.dp))
                Button(
                    onClick = {
                        val amount = limitAmount.toDoubleOrNull() ?: return@Button
                        viewModel.updateBudget(budgetId, name, amount)
                    },
                    enabled = name.isNotBlank() && limitAmount.isNotBlank(),
                    modifier = Modifier.fillMaxWidth().height(52.dp),
                ) { Text("Save Changes") }
            }
        }
    }
}
