package com.kakeibo.features.goals.presentation

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.outlined.Savings
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavController
import com.kakeibo.core.navigation.CreateGoalRoute
import com.kakeibo.core.navigation.GoalDetailRoute
import com.kakeibo.core.ui.EmptyState
import com.kakeibo.core.ui.ErrorState
import com.kakeibo.core.ui.LoadingState
import com.kakeibo.core.util.CurrencyFormatter
import com.kakeibo.features.goals.domain.Goal

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun GoalListScreen(
    navController: NavController,
    viewModel: GoalsViewModel = hiltViewModel(),
) {
    val uiState by viewModel.listState.collectAsStateWithLifecycle()

    Scaffold(
        topBar = { TopAppBar(title = { Text("Goals") }) },
        floatingActionButton = {
            FloatingActionButton(onClick = { navController.navigate(CreateGoalRoute) }) {
                Icon(Icons.Filled.Add, contentDescription = "Create goal")
            }
        },
    ) { padding ->
        PullToRefreshBox(
            isRefreshing = uiState is GoalListUiState.Loading,
            onRefresh = viewModel::loadGoals,
            modifier = Modifier.fillMaxSize().padding(padding),
        ) {
            when (val state = uiState) {
                is GoalListUiState.Loading -> LoadingState()
                is GoalListUiState.Error -> ErrorState(state.message, onRetry = viewModel::loadGoals)
                is GoalListUiState.Success -> {
                    if (state.goals.isEmpty()) {
                        EmptyState(
                            icon = Icons.Outlined.Savings,
                            title = "No goals yet",
                            subtitle = "Set a savings goal to track your progress.",
                        )
                    } else {
                        LazyColumn(
                            contentPadding = PaddingValues(16.dp),
                            verticalArrangement = Arrangement.spacedBy(12.dp),
                        ) {
                            items(state.goals) { goal ->
                                GoalCard(
                                    goal = goal,
                                    onClick = { navController.navigate(GoalDetailRoute(goal.id)) },
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun GoalCard(goal: Goal, onClick: () -> Unit) {
    Card(onClick = onClick, modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Text(
                    text = goal.name,
                    style = MaterialTheme.typography.titleSmall,
                    fontWeight = FontWeight.SemiBold,
                    modifier = Modifier.weight(1f),
                )
                if (goal.isAchieved) {
                    Text(
                        text = "✓ Achieved",
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.primary,
                    )
                } else {
                    Text(
                        text = "${goal.percentComplete.toInt()}%",
                        style = MaterialTheme.typography.labelMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }
            Spacer(modifier = Modifier.height(8.dp))
            LinearProgressIndicator(
                progress = { (goal.percentComplete / 100.0).toFloat().coerceIn(0f, 1f) },
                modifier = Modifier.fillMaxWidth(),
            )
            Spacer(modifier = Modifier.height(8.dp))
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
            ) {
                Text(
                    text = CurrencyFormatter.format(goal.currentAmount, goal.currency),
                    style = MaterialTheme.typography.bodySmall,
                )
                Text(
                    text = "of ${CurrencyFormatter.format(goal.targetAmount, goal.currency)}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            goal.deadline?.let {
                Text(
                    text = "Deadline: $it",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        }
    }
}
