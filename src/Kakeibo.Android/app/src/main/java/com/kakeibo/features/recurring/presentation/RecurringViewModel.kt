package com.kakeibo.features.recurring.presentation

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.kakeibo.core.api.ApiResult
import com.kakeibo.core.api.safeApiCall
import com.kakeibo.core.auth.TokenStore
import com.kakeibo.features.categories.data.CategoriesApi
import com.kakeibo.features.recurring.data.CreatePatternRequest
import com.kakeibo.features.recurring.data.RecurringApi
import com.kakeibo.features.recurring.data.UpdatePatternRequest
import com.kakeibo.features.recurring.domain.RecurringPattern
import com.kakeibo.features.wallets.data.WalletsApi
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed interface RecurringEvent {
    data object Saved : RecurringEvent
    data object Deleted : RecurringEvent
    data class Error(val message: String) : RecurringEvent
}

@HiltViewModel
class RecurringViewModel @Inject constructor(
    private val recurringApi: RecurringApi,
    private val walletsApi: WalletsApi,
    private val categoriesApi: CategoriesApi,
    private val tokenStore: TokenStore,
) : ViewModel() {

    private val _listState = MutableStateFlow<RecurringListUiState>(RecurringListUiState.Loading)
    val listState = _listState.asStateFlow()

    private val _createState = MutableStateFlow<CreatePatternUiState>(CreatePatternUiState.Idle)
    val createState = _createState.asStateFlow()

    private val _detailPattern = MutableStateFlow<RecurringPattern?>(null)
    val detailPattern = _detailPattern.asStateFlow()

    /** ISO-4217 currency code for the current user. Falls back to "USD" if not set. */
    val userCurrency: String get() = tokenStore.currency ?: "USD"

    private val _events = Channel<RecurringEvent>()
    val events = _events.receiveAsFlow()

    init {
        loadPatterns()
    }

    fun loadPatterns() {
        viewModelScope.launch {
            _listState.value = RecurringListUiState.Loading
            when (val result = safeApiCall { recurringApi.getPatterns() }) {
                is ApiResult.Success -> _listState.value = RecurringListUiState.Success(result.data)
                is ApiResult.Error -> _listState.value = RecurringListUiState.Error(result.message)
                is ApiResult.NetworkError -> _listState.value = RecurringListUiState.Error("Network error.")
            }
        }
    }

    fun loadCreateForm() {
        viewModelScope.launch {
            _createState.value = CreatePatternUiState.Loading
            val walletsResult = safeApiCall { walletsApi.getWallets() }
            val categoriesResult = safeApiCall { categoriesApi.getCategories() }
            if (walletsResult is ApiResult.Success && categoriesResult is ApiResult.Success) {
                _createState.value = CreatePatternUiState.Ready(
                    wallets = walletsResult.data,
                    categories = categoriesResult.data,
                )
            } else {
                _createState.value = CreatePatternUiState.Error("Failed to load form data.")
            }
        }
    }

    /**
     * Loads a single pattern into [detailPattern] for the edit screen.
     * First tries the already-loaded list to avoid a redundant network call.
     */
    fun loadPattern(id: String) {
        viewModelScope.launch {
            val fromList = (_listState.value as? RecurringListUiState.Success)
                ?.patterns?.find { it.id == id }
            if (fromList != null) {
                _detailPattern.value = fromList
            } else {
                when (val result = safeApiCall { recurringApi.getPattern(id) }) {
                    is ApiResult.Success -> _detailPattern.value = result.data
                    else -> { /* leave null; form stays blank */ }
                }
            }
        }
    }

    fun createPattern(
        description: String,
        amount: Double,
        type: String,
        categoryId: String,
        walletId: String,
        frequency: String,
        startDate: String,
    ) {
        viewModelScope.launch {
            when (val result = safeApiCall {
                recurringApi.createPattern(
                    CreatePatternRequest(description, amount, type, categoryId, walletId, frequency, startDate),
                )
            }) {
                is ApiResult.Success -> {
                    loadPatterns()
                    _events.send(RecurringEvent.Saved)
                }
                is ApiResult.Error -> _events.send(RecurringEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(RecurringEvent.Error("Network error."))
            }
        }
    }

    fun updatePattern(id: String, description: String, amount: Double, frequency: String) {
        viewModelScope.launch {
            when (val result = safeApiCall {
                recurringApi.updatePattern(id, UpdatePatternRequest(description, amount, frequency))
            }) {
                is ApiResult.Success -> {
                    loadPatterns()
                    _events.send(RecurringEvent.Saved)
                }
                is ApiResult.Error -> _events.send(RecurringEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(RecurringEvent.Error("Network error."))
            }
        }
    }

    fun deletePattern(id: String) {
        viewModelScope.launch {
            when (val result = safeApiCall { recurringApi.deletePattern(id) }) {
                is ApiResult.Success -> {
                    loadPatterns()
                    _events.send(RecurringEvent.Deleted)
                }
                is ApiResult.Error -> _events.send(RecurringEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(RecurringEvent.Error("Network error."))
            }
        }
    }
}
