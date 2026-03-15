package com.kakeibo.features.friends.presentation

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.PersonAdd
import androidx.compose.material.icons.outlined.Group
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
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
import com.kakeibo.features.friends.domain.Friend

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun FriendListScreen(
    navController: NavController,
    viewModel: FriendsViewModel = hiltViewModel(),
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    var showInviteDialog by remember { mutableStateOf(false) }
    var inviteEmail by remember { mutableStateOf("") }

    Scaffold(
        topBar = { TopAppBar(title = { Text("Friends") }) },
        floatingActionButton = {
            FloatingActionButton(onClick = { showInviteDialog = true }) {
                Icon(Icons.Filled.PersonAdd, contentDescription = "Add friend")
            }
        },
    ) { padding ->
        PullToRefreshBox(
            isRefreshing = uiState is FriendsUiState.Loading,
            onRefresh = viewModel::loadFriends,
            modifier = Modifier.fillMaxSize().padding(padding),
        ) {
            when (val state = uiState) {
                is FriendsUiState.Loading -> LoadingState()
                is FriendsUiState.Error -> ErrorState(state.message, onRetry = viewModel::loadFriends)
                is FriendsUiState.Success -> {
                    if (state.friends.isEmpty()) {
                        EmptyState(
                            icon = Icons.Outlined.Group,
                            title = "No friends yet",
                            subtitle = "Invite someone to manage shared expenses together.",
                        )
                    } else {
                        LazyColumn(
                            contentPadding = PaddingValues(16.dp),
                            verticalArrangement = Arrangement.spacedBy(8.dp),
                        ) {
                            items(state.friends) { friend ->
                                FriendCard(friend = friend)
                            }
                        }
                    }
                }
            }
        }
    }

    if (showInviteDialog) {
        AlertDialog(
            onDismissRequest = { showInviteDialog = false; inviteEmail = "" },
            title = { Text("Add Friend") },
            text = {
                OutlinedTextField(
                    value = inviteEmail,
                    onValueChange = { inviteEmail = it },
                    label = { Text("Email address") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth(),
                )
            },
            confirmButton = {
                TextButton(
                    onClick = {
                        if (inviteEmail.isNotBlank()) viewModel.sendRequest(inviteEmail)
                        showInviteDialog = false
                        inviteEmail = ""
                    },
                ) { Text("Send Request") }
            },
            dismissButton = {
                TextButton(onClick = { showInviteDialog = false; inviteEmail = "" }) { Text("Cancel") }
            },
        )
    }
}

@Composable
private fun FriendCard(friend: Friend) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.padding(16.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Icon(
                Icons.Outlined.Group,
                contentDescription = null,
                tint = MaterialTheme.colorScheme.primary,
            )
            Spacer(modifier = Modifier.width(12.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = friend.username,
                    style = MaterialTheme.typography.titleSmall,
                    fontWeight = FontWeight.SemiBold,
                )
                Text(
                    text = friend.email,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        }
    }
}
