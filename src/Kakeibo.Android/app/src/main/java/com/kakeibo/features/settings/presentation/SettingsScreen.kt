package com.kakeibo.features.settings.presentation

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ExitToApp
import androidx.compose.material.icons.filled.ChevronRight
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.Person
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavController
import com.kakeibo.core.ui.ErrorState
import com.kakeibo.core.ui.LoadingState
import kotlinx.coroutines.flow.collectLatest

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsScreen(
    navController: NavController,
    onLogout: () -> Unit,
    viewModel: SettingsViewModel = hiltViewModel(),
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }

    var showEditUsernameDialog by remember { mutableStateOf(false) }
    var showChangePasswordDialog by remember { mutableStateOf(false) }
    var showLogoutConfirmDialog by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) {
        viewModel.events.collectLatest { event ->
            when (event) {
                is SettingsEvent.LoggedOut, is SettingsEvent.AccountDeleted -> onLogout()
                is SettingsEvent.ProfileUpdated -> snackbarHostState.showSnackbar("Profile updated")
                is SettingsEvent.PasswordChanged -> snackbarHostState.showSnackbar("Password changed")
                is SettingsEvent.Error -> snackbarHostState.showSnackbar(event.message)
            }
        }
    }

    Scaffold(
        topBar = { TopAppBar(title = { Text("Settings") }) },
        snackbarHost = { SnackbarHost(snackbarHostState) },
    ) { padding ->
        when (val state = uiState) {
            is SettingsUiState.Loading -> LoadingState(modifier = Modifier.padding(padding))
            is SettingsUiState.Error -> ErrorState(
                message = state.message,
                onRetry = viewModel::loadProfile,
                modifier = Modifier.padding(padding),
            )
            is SettingsUiState.Success -> Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding)
                    .verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(16.dp),
            ) {
                Card(modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp).fillMaxWidth()) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Text(
                            text = state.user.username,
                            style = MaterialTheme.typography.titleLarge,
                            fontWeight = FontWeight.Bold,
                        )
                        Text(
                            text = state.user.email,
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                        Text(
                            text = "Currency: ${state.user.currency}",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                    }
                }

                Card(modifier = Modifier.padding(horizontal = 16.dp).fillMaxWidth()) {
                    Column {
                        ListItem(
                            headlineContent = { Text("Edit Username") },
                            leadingContent = { Icon(Icons.Filled.Person, contentDescription = null) },
                            trailingContent = { Icon(Icons.Filled.ChevronRight, contentDescription = null) },
                            modifier = Modifier.clickable { showEditUsernameDialog = true },
                        )
                        HorizontalDivider()
                        ListItem(
                            headlineContent = { Text("Change Password") },
                            leadingContent = { Icon(Icons.Filled.Lock, contentDescription = null) },
                            trailingContent = { Icon(Icons.Filled.ChevronRight, contentDescription = null) },
                            modifier = Modifier.clickable { showChangePasswordDialog = true },
                        )
                    }
                }

                Card(
                    modifier = Modifier.padding(horizontal = 16.dp).fillMaxWidth(),
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.errorContainer.copy(alpha = 0.3f),
                    ),
                ) {
                    ListItem(
                        headlineContent = {
                            Text("Sign Out", color = MaterialTheme.colorScheme.error)
                        },
                        leadingContent = {
                            Icon(
                                Icons.AutoMirrored.Filled.ExitToApp,
                                contentDescription = null,
                                tint = MaterialTheme.colorScheme.error,
                            )
                        },
                        modifier = Modifier.clickable { showLogoutConfirmDialog = true },
                    )
                }
                Spacer(modifier = Modifier.height(24.dp))
            }
        }
    }

    if (showLogoutConfirmDialog) {
        AlertDialog(
            onDismissRequest = { showLogoutConfirmDialog = false },
            title = { Text("Sign Out") },
            text = { Text("Are you sure you want to sign out?") },
            confirmButton = {
                TextButton(onClick = { showLogoutConfirmDialog = false; viewModel.logout() }) {
                    Text("Sign Out", color = MaterialTheme.colorScheme.error)
                }
            },
            dismissButton = {
                TextButton(onClick = { showLogoutConfirmDialog = false }) { Text("Cancel") }
            },
        )
    }

    if (showEditUsernameDialog) {
        EditUsernameDialog(
            currentUsername = (uiState as? SettingsUiState.Success)?.user?.username ?: "",
            onConfirm = { viewModel.updateProfile(it); showEditUsernameDialog = false },
            onDismiss = { showEditUsernameDialog = false },
        )
    }

    if (showChangePasswordDialog) {
        ChangePasswordDialog(
            onConfirm = { current, new -> viewModel.changePassword(current, new); showChangePasswordDialog = false },
            onDismiss = { showChangePasswordDialog = false },
        )
    }
}

@Composable
private fun EditUsernameDialog(
    currentUsername: String,
    onConfirm: (String) -> Unit,
    onDismiss: () -> Unit,
) {
    var username by remember { mutableStateOf(currentUsername) }
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Edit Username") },
        text = {
            OutlinedTextField(
                value = username,
                onValueChange = { username = it },
                label = { Text("Username") },
                singleLine = true,
                modifier = Modifier.fillMaxWidth(),
            )
        },
        confirmButton = {
            TextButton(onClick = { if (username.isNotBlank()) onConfirm(username) }) { Text("Save") }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Cancel") } },
    )
}

@Composable
private fun ChangePasswordDialog(
    onConfirm: (String, String) -> Unit,
    onDismiss: () -> Unit,
) {
    var current by remember { mutableStateOf("") }
    var newPwd by remember { mutableStateOf("") }
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Change Password") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedTextField(
                    value = current,
                    onValueChange = { current = it },
                    label = { Text("Current Password") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth(),
                )
                OutlinedTextField(
                    value = newPwd,
                    onValueChange = { newPwd = it },
                    label = { Text("New Password") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth(),
                )
            }
        },
        confirmButton = {
            TextButton(
                onClick = { if (current.isNotBlank() && newPwd.length >= 8) onConfirm(current, newPwd) },
            ) { Text("Change") }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Cancel") } },
    )
}
