package com.kakeibo.features.categories.presentation

import androidx.compose.foundation.layout.Arrangement
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
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material.icons.outlined.Category
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SuggestionChip
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
import com.kakeibo.core.navigation.CreateCategoryRoute
import com.kakeibo.core.navigation.EditCategoryRoute
import com.kakeibo.core.ui.CategoryColors
import com.kakeibo.core.ui.CategoryIcon
import com.kakeibo.core.ui.EmptyState
import com.kakeibo.core.ui.ErrorState
import com.kakeibo.core.ui.LoadingState
import com.kakeibo.features.categories.domain.Category

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CategoryListScreen(
    navController: NavController,
    viewModel: CategoriesViewModel = hiltViewModel(),
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()

    Scaffold(
        topBar = { TopAppBar(title = { Text("Categories") }) },
        floatingActionButton = {
            FloatingActionButton(onClick = { navController.navigate(CreateCategoryRoute) }) {
                Icon(Icons.Filled.Add, contentDescription = "Create category")
            }
        },
    ) { padding ->
        PullToRefreshBox(
            isRefreshing = uiState is CategoryListUiState.Loading,
            onRefresh = viewModel::loadCategories,
            modifier = Modifier.fillMaxSize().padding(padding),
        ) {
            when (val state = uiState) {
                is CategoryListUiState.Loading -> LoadingState()
                is CategoryListUiState.Error -> ErrorState(state.message, onRetry = viewModel::loadCategories)
                is CategoryListUiState.Success -> {
                    if (state.categories.isEmpty()) {
                        EmptyState(
                            icon = Icons.Outlined.Category,
                            title = "No custom categories",
                            subtitle = "System categories are always available.",
                        )
                    } else {
                        LazyColumn(
                            contentPadding = PaddingValues(16.dp),
                            verticalArrangement = Arrangement.spacedBy(8.dp),
                        ) {
                            items(state.categories) { category ->
                                CategoryCard(
                                    category = category,
                                    onEdit = { navController.navigate(EditCategoryRoute(category.id)) },
                                    onDelete = { if (!category.isSystem) viewModel.deleteCategory(category.id) },
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
private fun CategoryCard(
    category: Category,
    onEdit: () -> Unit,
    onDelete: () -> Unit,
) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.padding(12.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            CategoryIcon(
                color = CategoryColors.fromHex(category.color)
                    .takeIf { it != CategoryColors.Other }
                    ?: CategoryColors.forName(category.name),
                size = 40.dp,
            )
            Spacer(modifier = Modifier.width(12.dp))
            Text(
                text = category.name,
                style = MaterialTheme.typography.bodyLarge,
                fontWeight = FontWeight.Medium,
                modifier = Modifier.weight(1f),
            )
            if (category.isSystem) {
                SuggestionChip(onClick = {}, label = { Text("System", style = MaterialTheme.typography.labelSmall) })
            } else {
                IconButton(onClick = onEdit) {
                    Icon(Icons.Filled.Edit, contentDescription = "Edit", tint = MaterialTheme.colorScheme.primary)
                }
                IconButton(onClick = onDelete) {
                    Icon(Icons.Filled.Delete, contentDescription = "Delete", tint = MaterialTheme.colorScheme.error)
                }
            }
        }
    }
}
