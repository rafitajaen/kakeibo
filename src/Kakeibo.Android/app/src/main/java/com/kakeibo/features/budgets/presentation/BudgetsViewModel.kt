package com.kakeibo.features.budgets.presentation

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.kakeibo.core.api.ApiResult
import com.kakeibo.core.api.safeApiCall
import com.kakeibo.features.budgets.data.BudgetsApi
import com.kakeibo.features.budgets.data.CreateBudgetRequest
import com.kakeibo.features.budgets.data.UpdateBudgetRequest
import com.kakeibo.features.categories.data.CategoriesApi
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed interface BudgetEvent {
    data object Saved : BudgetEvent
    data object Deleted : BudgetEvent
    data class Error(val message: String) : BudgetEvent
}

@HiltViewModel
class BudgetsViewModel @Inject constructor(
    private val budgetsApi: BudgetsApi,
    private val categoriesApi: CategoriesApi,
) : ViewModel() {

    private val _listState = MutableStateFlow<BudgetListUiState>(BudgetListUiState.Loading)
    val listState = _listState.asStateFlow()

    private val _detailState = MutableStateFlow<BudgetDetailUiState>(BudgetDetailUiState.Loading)
    val detailState = _detailState.asStateFlow()

    private val _createState = MutableStateFlow<CreateBudgetUiState>(CreateBudgetUiState.Idle)
    val createState = _createState.asStateFlow()

    private val _events = Channel<BudgetEvent>()
    val events = _events.receiveAsFlow()

    init {
        loadBudgets()
    }

    fun loadBudgets() {
        viewModelScope.launch {
            _listState.value = BudgetListUiState.Loading
            when (val result = safeApiCall { budgetsApi.getBudgets() }) {
                is ApiResult.Success -> _listState.value = BudgetListUiState.Success(result.data)
                is ApiResult.Error -> _listState.value = BudgetListUiState.Error(result.message)
                is ApiResult.NetworkError -> _listState.value = BudgetListUiState.Error("Network error.")
            }
        }
    }

    fun loadDetail(id: String) {
        viewModelScope.launch {
            _detailState.value = BudgetDetailUiState.Loading
            when (val result = safeApiCall { budgetsApi.getBudget(id) }) {
                is ApiResult.Success -> _detailState.value = BudgetDetailUiState.Success(result.data)
                is ApiResult.Error -> _detailState.value = BudgetDetailUiState.Error(result.message)
                is ApiResult.NetworkError -> _detailState.value = BudgetDetailUiState.Error("Network error.")
            }
        }
    }

    fun loadCreateForm() {
        viewModelScope.launch {
            _createState.value = CreateBudgetUiState.Loading
            when (val result = safeApiCall { categoriesApi.getCategories() }) {
                is ApiResult.Success -> _createState.value = CreateBudgetUiState.Ready(result.data)
                is ApiResult.Error -> _createState.value = CreateBudgetUiState.Error(result.message)
                is ApiResult.NetworkError -> _createState.value = CreateBudgetUiState.Error("Network error.")
            }
        }
    }

    fun createBudget(
        name: String,
        categoryId: String,
        limitAmount: Double,
        periodStart: String,
        periodEnd: String,
    ) {
        viewModelScope.launch {
            when (val result = safeApiCall {
                budgetsApi.createBudget(
                    CreateBudgetRequest(name, categoryId, limitAmount, periodStart, periodEnd),
                )
            }) {
                is ApiResult.Success -> {
                    loadBudgets()
                    _events.send(BudgetEvent.Saved)
                }
                is ApiResult.Error -> _events.send(BudgetEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(BudgetEvent.Error("Network error."))
            }
        }
    }

    fun updateBudget(id: String, name: String, limitAmount: Double) {
        viewModelScope.launch {
            when (val result = safeApiCall {
                budgetsApi.updateBudget(id, UpdateBudgetRequest(name, limitAmount))
            }) {
                is ApiResult.Success -> {
                    loadBudgets()
                    _events.send(BudgetEvent.Saved)
                }
                is ApiResult.Error -> _events.send(BudgetEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(BudgetEvent.Error("Network error."))
            }
        }
    }

    fun deleteBudget(id: String) {
        viewModelScope.launch {
            when (val result = safeApiCall { budgetsApi.deleteBudget(id) }) {
                is ApiResult.Success -> {
                    loadBudgets()
                    _events.send(BudgetEvent.Deleted)
                }
                is ApiResult.Error -> _events.send(BudgetEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(BudgetEvent.Error("Network error."))
            }
        }
    }
}
