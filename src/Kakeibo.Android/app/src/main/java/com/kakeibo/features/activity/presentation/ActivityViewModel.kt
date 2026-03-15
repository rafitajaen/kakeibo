package com.kakeibo.features.activity.presentation

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.kakeibo.core.api.ApiResult
import com.kakeibo.core.api.safeApiCall
import com.kakeibo.features.activity.data.ActivityApi
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class ActivityViewModel @Inject constructor(
    private val activityApi: ActivityApi,
) : ViewModel() {

    private val _uiState = MutableStateFlow<ActivityUiState>(ActivityUiState.Loading)
    val uiState = _uiState.asStateFlow()

    init {
        loadFeed()
    }

    fun loadFeed() {
        viewModelScope.launch {
            _uiState.value = ActivityUiState.Loading
            when (val result = safeApiCall { activityApi.getActivityFeed() }) {
                is ApiResult.Success -> _uiState.value = ActivityUiState.Success(result.data)
                is ApiResult.Error -> _uiState.value = ActivityUiState.Error(result.message)
                is ApiResult.NetworkError -> _uiState.value = ActivityUiState.Error("Network error.")
            }
        }
    }
}
