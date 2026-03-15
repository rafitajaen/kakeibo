package com.kakeibo.features.goals.presentation

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
fun EditGoalScreen(
    goalId: String,
    navController: NavController,
    viewModel: GoalsViewModel = hiltViewModel(),
) {
    val uiState by viewModel.detailState.collectAsStateWithLifecycle()

    var name by remember { mutableStateOf("") }
    var targetAmount by remember { mutableStateOf("") }
    var deadline by remember { mutableStateOf("") }
    var initialized by remember { mutableStateOf(false) }

    LaunchedEffect(goalId) { viewModel.loadDetail(goalId) }
    LaunchedEffect(uiState) {
        if (!initialized && uiState is GoalDetailUiState.Success) {
            val goal = (uiState as GoalDetailUiState.Success).goal
            name = goal.name
            targetAmount = goal.targetAmount.toString()
            deadline = goal.deadline ?: ""
            initialized = true
        }
    }
    LaunchedEffect(Unit) {
        viewModel.events.collectLatest { event ->
            if (event is GoalEvent.Saved) navController.popBackStack()
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Edit Goal") },
                navigationIcon = {
                    IconButton(onClick = { navController.popBackStack() }) {
                        Icon(Icons.Filled.ArrowBack, contentDescription = "Back")
                    }
                },
            )
        },
    ) { padding ->
        when (val state = uiState) {
            is GoalDetailUiState.Loading -> LoadingState(modifier = Modifier.padding(padding))
            is GoalDetailUiState.Error -> ErrorState(
                message = state.message,
                onRetry = { viewModel.loadDetail(goalId) },
                modifier = Modifier.padding(padding),
            )
            is GoalDetailUiState.Success -> Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding)
                    .padding(horizontal = 24.dp),
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
                Spacer(modifier = Modifier.height(32.dp))
                Button(
                    onClick = {
                        val amount = targetAmount.toDoubleOrNull() ?: return@Button
                        viewModel.updateGoal(goalId, name, amount, deadline.takeIf { it.isNotBlank() })
                    },
                    enabled = name.isNotBlank() && targetAmount.isNotBlank(),
                    modifier = Modifier.fillMaxWidth().height(52.dp),
                ) { Text("Save Changes") }
            }
        }
    }
}
