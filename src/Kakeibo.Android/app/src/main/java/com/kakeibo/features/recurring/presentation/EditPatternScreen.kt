package com.kakeibo.features.recurring.presentation

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
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
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
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
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.navigation.NavController
import kotlinx.coroutines.flow.collectLatest

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun EditPatternScreen(
    patternId: String,
    navController: NavController,
    viewModel: RecurringViewModel = hiltViewModel(),
) {
    val detailPattern by viewModel.detailPattern.collectAsStateWithLifecycle()
    var description by remember { mutableStateOf("") }
    var amount by remember { mutableStateOf("") }
    var frequency by remember { mutableStateOf("MONTHLY") }

    // Fetch pattern data when the screen opens
    LaunchedEffect(patternId) {
        viewModel.loadPattern(patternId)
    }

    // Pre-populate fields when the pattern data arrives
    LaunchedEffect(detailPattern) {
        detailPattern?.let { p ->
            description = p.description
            amount = p.amount.toString()
            frequency = p.frequency
        }
    }

    LaunchedEffect(Unit) {
        viewModel.events.collectLatest { event ->
            if (event is RecurringEvent.Saved) navController.popBackStack()
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Edit Pattern") },
                navigationIcon = {
                    IconButton(onClick = { navController.popBackStack() }) {
                        Icon(Icons.Filled.ArrowBack, contentDescription = "Back")
                    }
                },
            )
        },
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .padding(horizontal = 24.dp),
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
            Spacer(modifier = Modifier.height(32.dp))
            Button(
                onClick = {
                    val amt = amount.toDoubleOrNull() ?: return@Button
                    viewModel.updatePattern(patternId, description, amt, frequency)
                },
                enabled = description.isNotBlank() && amount.isNotBlank(),
                modifier = Modifier.fillMaxWidth().height(52.dp),
            ) { Text("Save Changes") }
        }
    }
}
