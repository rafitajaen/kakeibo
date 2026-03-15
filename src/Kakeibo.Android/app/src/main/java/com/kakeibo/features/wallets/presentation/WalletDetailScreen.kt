package com.kakeibo.features.wallets.presentation

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.PersonAdd
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavController
import com.kakeibo.core.navigation.InviteMemberRoute
import com.kakeibo.core.ui.AmountText
import com.kakeibo.core.ui.AmountType
import com.kakeibo.core.ui.CategoryColors
import com.kakeibo.core.ui.ErrorState
import com.kakeibo.core.ui.LoadingState
import com.kakeibo.core.ui.WalletIcon
import com.kakeibo.core.util.CurrencyFormatter
import com.kakeibo.features.wallets.domain.Wallet

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun WalletDetailScreen(
    walletId: String,
    navController: NavController,
    viewModel: WalletsViewModel = hiltViewModel(),
) {
    val uiState by viewModel.detailState.collectAsStateWithLifecycle()

    LaunchedEffect(walletId) {
        viewModel.loadDetail(walletId)
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    when (val s = uiState) {
                        is WalletDetailUiState.Success -> Text(s.wallet.name)
                        else -> Text("Wallet")
                    }
                },
                navigationIcon = {
                    IconButton(onClick = { navController.popBackStack() }) {
                        Icon(Icons.Filled.ArrowBack, contentDescription = "Back")
                    }
                },
                actions = {
                    if (uiState is WalletDetailUiState.Success) {
                        val wallet = (uiState as WalletDetailUiState.Success).wallet
                        if (wallet.type.name == "SHARED") {
                            IconButton(onClick = { navController.navigate(InviteMemberRoute(walletId)) }) {
                                Icon(Icons.Filled.PersonAdd, contentDescription = "Invite member")
                            }
                        }
                    }
                },
            )
        },
    ) { padding ->
        when (val state = uiState) {
            is WalletDetailUiState.Loading -> LoadingState(modifier = Modifier.padding(padding))
            is WalletDetailUiState.Error -> ErrorState(
                message = state.message,
                onRetry = { viewModel.loadDetail(walletId) },
                modifier = Modifier.padding(padding),
            )
            is WalletDetailUiState.Success -> WalletDetailContent(
                wallet = state.wallet,
                members = state.members,
                modifier = Modifier.padding(padding),
            )
        }
    }
}

@Composable
private fun WalletDetailContent(
    wallet: Wallet,
    members: List<com.kakeibo.features.wallets.domain.WalletMember>,
    modifier: Modifier = Modifier,
) {
    LazyColumn(modifier = modifier.fillMaxSize()) {
        item {
            // Balance header
            Card(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(16.dp),
            ) {
                Row(
                    modifier = Modifier.padding(20.dp),
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    WalletIcon(
                        color = CategoryColors.fromHex(wallet.color),
                        size = 56.dp,
                    )
                    Spacer(modifier = Modifier.width(16.dp))
                    Column {
                        Text(
                            text = wallet.name,
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.Bold,
                        )
                        Spacer(modifier = Modifier.height(4.dp))
                        AmountText(
                            amount = CurrencyFormatter.format(wallet.balance, wallet.currency),
                            type = if (wallet.balance >= 0) AmountType.INCOME else AmountType.EXPENSE,
                            style = MaterialTheme.typography.titleLarge,
                        )
                    }
                }
            }
        }
        if (members.isNotEmpty()) {
            item {
                Text(
                    text = "Members",
                    style = MaterialTheme.typography.titleSmall,
                    fontWeight = FontWeight.SemiBold,
                    modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
                )
            }
            items(members) { member ->
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 16.dp, vertical = 8.dp),
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Icon(
                        Icons.Filled.Person,
                        contentDescription = null,
                        modifier = Modifier.size(20.dp),
                        tint = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                    Spacer(modifier = Modifier.width(8.dp))
                    Column {
                        Text(text = member.username, style = MaterialTheme.typography.bodyLarge)
                        Text(
                            text = member.email,
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                    }
                }
            }
        }
    }
}
