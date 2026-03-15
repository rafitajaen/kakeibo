package com.kakeibo.features.wallets.presentation

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
import androidx.compose.material.icons.filled.AccountBalanceWallet
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.outlined.AccountBalanceWallet
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
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
import com.kakeibo.core.navigation.CreateWalletRoute
import com.kakeibo.core.navigation.WalletDetailRoute
import com.kakeibo.core.ui.AmountText
import com.kakeibo.core.ui.AmountType
import com.kakeibo.core.ui.CategoryColors
import com.kakeibo.core.ui.EmptyState
import com.kakeibo.core.ui.ErrorState
import com.kakeibo.core.ui.LoadingState
import com.kakeibo.core.ui.WalletIcon
import com.kakeibo.core.util.CurrencyFormatter
import com.kakeibo.features.wallets.domain.Wallet

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun WalletListScreen(
    navController: NavController,
    viewModel: WalletsViewModel = hiltViewModel(),
) {
    val uiState by viewModel.listState.collectAsStateWithLifecycle()

    Scaffold(
        topBar = { TopAppBar(title = { Text("Wallets") }) },
        floatingActionButton = {
            FloatingActionButton(onClick = { navController.navigate(CreateWalletRoute) }) {
                Icon(Icons.Filled.Add, contentDescription = "Add wallet")
            }
        },
    ) { padding ->
        PullToRefreshBox(
            isRefreshing = uiState is WalletListUiState.Loading,
            onRefresh = viewModel::loadWallets,
            modifier = Modifier.fillMaxSize().padding(padding),
        ) {
            when (val state = uiState) {
                is WalletListUiState.Loading -> LoadingState()
                is WalletListUiState.Error -> ErrorState(state.message, onRetry = viewModel::loadWallets)
                is WalletListUiState.Success -> {
                    if (state.wallets.isEmpty()) {
                        EmptyState(
                            icon = Icons.Outlined.AccountBalanceWallet,
                            title = "No wallets yet",
                            subtitle = "Create your first wallet to start tracking.",
                        )
                    } else {
                        LazyColumn(
                            contentPadding = PaddingValues(16.dp),
                            verticalArrangement = Arrangement.spacedBy(12.dp),
                        ) {
                            items(state.wallets) { wallet ->
                                WalletCard(
                                    wallet = wallet,
                                    onClick = { navController.navigate(WalletDetailRoute(wallet.id)) },
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
private fun WalletCard(wallet: Wallet, onClick: () -> Unit) {
    Card(onClick = onClick, modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.padding(16.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            WalletIcon(
                color = CategoryColors.fromHex(wallet.color),
                icon = Icons.Filled.AccountBalanceWallet,
                size = 48.dp,
            )
            Spacer(modifier = Modifier.width(12.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = wallet.name,
                    style = MaterialTheme.typography.titleSmall,
                    fontWeight = FontWeight.SemiBold,
                )
                Text(
                    text = "${wallet.type.name.lowercase().replaceFirstChar { it.uppercase() }}  •  ${wallet.memberCount} member(s)",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            AmountText(
                amount = CurrencyFormatter.format(wallet.balance, wallet.currency),
                type = if (wallet.balance >= 0) AmountType.INCOME else AmountType.EXPENSE,
                style = MaterialTheme.typography.titleSmall,
            )
        }
    }
}
