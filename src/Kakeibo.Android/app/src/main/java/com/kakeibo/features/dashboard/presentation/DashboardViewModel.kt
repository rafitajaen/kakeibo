package com.kakeibo.features.dashboard.presentation

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.kakeibo.core.api.ApiResult
import com.kakeibo.core.api.safeApiCall
import com.kakeibo.features.transactions.data.TransactionsApi
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class DashboardViewModel @Inject constructor(
    private val transactionsApi: TransactionsApi,
) : ViewModel() {

    private val _uiState = MutableStateFlow<DashboardUiState>(DashboardUiState.Loading)
    val uiState = _uiState.asStateFlow()

    init {
        load()
    }

    fun load() {
        viewModelScope.launch {
            _uiState.value = DashboardUiState.Loading
            when (val result = safeApiCall { transactionsApi.getDashboardSummary() }) {
                is ApiResult.Success -> _uiState.value = DashboardUiState.Success(result.data)
                is ApiResult.Error -> _uiState.value = DashboardUiState.Error(result.message)
                is ApiResult.NetworkError -> _uiState.value = DashboardUiState.Error("Network error. Check your connection.")
            }
        }
    }
}
