package com.kakeibo.features.transactions.presentation

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.kakeibo.core.api.ApiResult
import com.kakeibo.core.api.safeApiCall
import com.kakeibo.core.util.DateFormatter
import com.kakeibo.features.categories.data.CategoriesApi
import com.kakeibo.features.transactions.data.CreateTransactionRequest
import com.kakeibo.features.transactions.data.TransactionsApi
import com.kakeibo.features.wallets.data.WalletsApi
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed interface TransactionEvent {
    data object Saved : TransactionEvent
    data object Deleted : TransactionEvent
    data class Error(val message: String) : TransactionEvent
}

@HiltViewModel
class TransactionsViewModel @Inject constructor(
    private val transactionsApi: TransactionsApi,
    private val walletsApi: WalletsApi,
    private val categoriesApi: CategoriesApi,
) : ViewModel() {

    private val _uiState = MutableStateFlow<TransactionsUiState>(TransactionsUiState.Loading)
    val uiState = _uiState.asStateFlow()

    private val _formState = MutableStateFlow<RecordTransactionUiState>(RecordTransactionUiState.Idle)
    val formState = _formState.asStateFlow()

    private val _events = Channel<TransactionEvent>()
    val events = _events.receiveAsFlow()

    init {
        loadTransactions()
    }

    fun loadTransactions(walletId: String? = null) {
        viewModelScope.launch {
            _uiState.value = TransactionsUiState.Loading
            when (val result = safeApiCall { transactionsApi.getTransactions(walletId = walletId) }) {
                is ApiResult.Success -> {
                    val transactions = result.data.items
                    val grouped = transactions
                        .groupBy { DateFormatter.toLocalDate(DateFormatter.parseInstant(it.date)) }
                        .toSortedMap(compareByDescending { it })
                    val income = transactions.filter { it.type.name == "INCOME" }.sumOf { it.amount }
                    val expenses = transactions.filter { it.type.name == "EXPENSE" }.sumOf { it.amount }
                    _uiState.value = TransactionsUiState.Success(
                        groupedTransactions = grouped,
                        totalIncome = income,
                        totalExpenses = expenses,
                        currency = transactions.firstOrNull()?.let { "USD" } ?: "USD",
                    )
                }
                is ApiResult.Error -> _uiState.value = TransactionsUiState.Error(result.message)
                is ApiResult.NetworkError -> _uiState.value = TransactionsUiState.Error("Network error.")
            }
        }
    }

    fun loadFormData() {
        viewModelScope.launch {
            _formState.value = RecordTransactionUiState.Loading
            val walletsResult = safeApiCall { walletsApi.getWallets() }
            val categoriesResult = safeApiCall { categoriesApi.getCategories() }
            if (walletsResult is ApiResult.Success && categoriesResult is ApiResult.Success) {
                _formState.value = RecordTransactionUiState.Ready(
                    wallets = walletsResult.data,
                    categories = categoriesResult.data,
                )
            } else {
                _formState.value = RecordTransactionUiState.Error("Failed to load form data.")
            }
        }
    }

    fun createTransaction(
        walletId: String,
        type: String,
        amount: Double,
        description: String,
        categoryId: String,
        date: String,
        notes: String = "",
        destinationWalletId: String? = null,
    ) {
        viewModelScope.launch {
            when (val result = safeApiCall {
                transactionsApi.createTransaction(
                    CreateTransactionRequest(
                        walletId = walletId,
                        type = type,
                        amount = amount,
                        description = description,
                        categoryId = categoryId,
                        date = date,
                        notes = notes,
                        destinationWalletId = destinationWalletId,
                    ),
                )
            }) {
                is ApiResult.Success -> {
                    loadTransactions()
                    _events.send(TransactionEvent.Saved)
                }
                is ApiResult.Error -> _events.send(TransactionEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(TransactionEvent.Error("Network error."))
            }
        }
    }

    fun deleteTransaction(id: String) {
        viewModelScope.launch {
            when (val result = safeApiCall { transactionsApi.deleteTransaction(id) }) {
                is ApiResult.Success -> {
                    loadTransactions()
                    _events.send(TransactionEvent.Deleted)
                }
                is ApiResult.Error -> _events.send(TransactionEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(TransactionEvent.Error("Network error."))
            }
        }
    }
}
