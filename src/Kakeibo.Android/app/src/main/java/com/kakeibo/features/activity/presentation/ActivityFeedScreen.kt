package com.kakeibo.features.activity.presentation

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.BarChart
import androidx.compose.material.icons.outlined.History
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
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
import com.kakeibo.core.ui.EmptyState
import com.kakeibo.core.ui.ErrorState
import com.kakeibo.core.ui.LoadingState
import com.kakeibo.features.activity.domain.ActivityEntry

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ActivityFeedScreen(
    navController: NavController,
    viewModel: ActivityViewModel = hiltViewModel(),
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()

    Scaffold(
        topBar = { TopAppBar(title = { Text("Activity") }) },
    ) { padding ->
        PullToRefreshBox(
            isRefreshing = uiState is ActivityUiState.Loading,
            onRefresh = viewModel::loadFeed,
            modifier = Modifier.fillMaxSize().padding(padding),
        ) {
            when (val state = uiState) {
                is ActivityUiState.Loading -> LoadingState()
                is ActivityUiState.Error -> ErrorState(state.message, onRetry = viewModel::loadFeed)
                is ActivityUiState.Success -> {
                    if (state.entries.isEmpty()) {
                        EmptyState(
                            icon = Icons.Outlined.BarChart,
                            title = "No activity yet",
                            subtitle = "Your financial activity will appear here.",
                        )
                    } else {
                        LazyColumn(
                            contentPadding = PaddingValues(16.dp),
                            verticalArrangement = Arrangement.spacedBy(8.dp),
                        ) {
                            items(state.entries) { entry ->
                                ActivityEntryCard(entry = entry)
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun ActivityEntryCard(entry: ActivityEntry) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.padding(16.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Icon(
                Icons.Outlined.History,
                contentDescription = null,
                modifier = Modifier.size(20.dp),
                tint = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            Spacer(modifier = Modifier.width(12.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = entry.description,
                    style = MaterialTheme.typography.bodyMedium,
                    fontWeight = FontWeight.Medium,
                )
                Text(
                    text = entry.occurredAt,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        }
    }
}
