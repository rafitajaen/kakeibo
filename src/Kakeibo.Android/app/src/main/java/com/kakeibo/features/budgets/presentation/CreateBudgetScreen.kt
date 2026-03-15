package com.kakeibo.features.budgets.presentation

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
import kotlinx.datetime.Clock
import kotlinx.datetime.TimeZone
import kotlinx.datetime.todayIn

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CreateBudgetScreen(
    navController: NavController,
    viewModel: BudgetsViewModel = hiltViewModel(),
) {
    val formState by viewModel.createState.collectAsStateWithLifecycle()

    var name by remember { mutableStateOf("") }
    var selectedCategoryId by remember { mutableStateOf("") }
    var selectedCategoryName by remember { mutableStateOf("") }
    var limitAmount by remember { mutableStateOf("") }
    var categoryExpanded by remember { mutableStateOf(false) }

    val today = Clock.System.todayIn(TimeZone.currentSystemDefault())
    val periodStart = today.toString()
    val periodEnd = today.let {
        kotlinx.datetime.LocalDate(it.year, it.month, it.month.length(false))
    }.toString()

    LaunchedEffect(Unit) {
        viewModel.loadCreateForm()
        viewModel.events.collectLatest { event ->
            if (event is BudgetEvent.Saved) navController.popBackStack()
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("New Budget") },
                navigationIcon = {
                    IconButton(onClick = { navController.popBackStack() }) {
                        Icon(Icons.Filled.ArrowBack, contentDescription = "Back")
                    }
                },
            )
        },
    ) { padding ->
        when (val state = formState) {
            is CreateBudgetUiState.Loading -> LoadingState(modifier = Modifier.padding(padding))
            is CreateBudgetUiState.Error -> ErrorState(
                message = state.message,
                onRetry = viewModel::loadCreateForm,
                modifier = Modifier.padding(padding),
            )
            else -> {
                val categories = if (state is CreateBudgetUiState.Ready) state.categories else emptyList()
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
                        label = { Text("Budget Name") },
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth(),
                    )
                    Spacer(modifier = Modifier.height(16.dp))
                    ExposedDropdownMenuBox(
                        expanded = categoryExpanded,
                        onExpandedChange = { categoryExpanded = it },
                    ) {
                        OutlinedTextField(
                            value = selectedCategoryName.ifBlank { "Select category" },
                            onValueChange = {},
                            readOnly = true,
                            label = { Text("Category") },
                            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = categoryExpanded) },
                            modifier = Modifier
                                .menuAnchor(MenuAnchorType.PrimaryNotEditable)
                                .fillMaxWidth(),
                        )
                        ExposedDropdownMenu(
                            expanded = categoryExpanded,
                            onDismissRequest = { categoryExpanded = false },
                        ) {
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
                            viewModel.createBudget(name, selectedCategoryId, amount, periodStart, periodEnd)
                        },
                        enabled = name.isNotBlank() && selectedCategoryId.isNotBlank() && limitAmount.isNotBlank(),
                        modifier = Modifier.fillMaxWidth().height(52.dp),
                    ) { Text("Create Budget") }
                    Spacer(modifier = Modifier.height(24.dp))
                }
            }
        }
    }
}
