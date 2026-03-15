package com.kakeibo.features.goals.presentation

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.kakeibo.core.api.ApiResult
import com.kakeibo.core.api.safeApiCall
import com.kakeibo.features.goals.data.CreateGoalRequest
import com.kakeibo.features.goals.data.GoalsApi
import com.kakeibo.features.goals.data.UpdateGoalRequest
import com.kakeibo.features.wallets.data.WalletsApi
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed interface GoalEvent {
    data object Saved : GoalEvent
    data object Deleted : GoalEvent
    data class Error(val message: String) : GoalEvent
}

@HiltViewModel
class GoalsViewModel @Inject constructor(
    private val goalsApi: GoalsApi,
    private val walletsApi: WalletsApi,
) : ViewModel() {

    private val _listState = MutableStateFlow<GoalListUiState>(GoalListUiState.Loading)
    val listState = _listState.asStateFlow()

    private val _detailState = MutableStateFlow<GoalDetailUiState>(GoalDetailUiState.Loading)
    val detailState = _detailState.asStateFlow()

    private val _createState = MutableStateFlow<CreateGoalUiState>(CreateGoalUiState.Idle)
    val createState = _createState.asStateFlow()

    private val _events = Channel<GoalEvent>()
    val events = _events.receiveAsFlow()

    init {
        loadGoals()
    }

    fun loadGoals() {
        viewModelScope.launch {
            _listState.value = GoalListUiState.Loading
            when (val result = safeApiCall { goalsApi.getGoals() }) {
                is ApiResult.Success -> _listState.value = GoalListUiState.Success(result.data)
                is ApiResult.Error -> _listState.value = GoalListUiState.Error(result.message)
                is ApiResult.NetworkError -> _listState.value = GoalListUiState.Error("Network error.")
            }
        }
    }

    fun loadDetail(id: String) {
        viewModelScope.launch {
            _detailState.value = GoalDetailUiState.Loading
            when (val result = safeApiCall { goalsApi.getGoal(id) }) {
                is ApiResult.Success -> _detailState.value = GoalDetailUiState.Success(result.data)
                is ApiResult.Error -> _detailState.value = GoalDetailUiState.Error(result.message)
                is ApiResult.NetworkError -> _detailState.value = GoalDetailUiState.Error("Network error.")
            }
        }
    }

    fun loadCreateForm() {
        viewModelScope.launch {
            _createState.value = CreateGoalUiState.Loading
            when (val result = safeApiCall { walletsApi.getWallets() }) {
                is ApiResult.Success -> _createState.value = CreateGoalUiState.Ready(result.data)
                is ApiResult.Error -> _createState.value = CreateGoalUiState.Error(result.message)
                is ApiResult.NetworkError -> _createState.value = CreateGoalUiState.Error("Network error.")
            }
        }
    }

    fun createGoal(
        name: String,
        targetAmount: Double,
        deadline: String?,
        linkedWalletId: String?,
    ) {
        viewModelScope.launch {
            when (val result = safeApiCall {
                goalsApi.createGoal(CreateGoalRequest(name, targetAmount, deadline, linkedWalletId))
            }) {
                is ApiResult.Success -> {
                    loadGoals()
                    _events.send(GoalEvent.Saved)
                }
                is ApiResult.Error -> _events.send(GoalEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(GoalEvent.Error("Network error."))
            }
        }
    }

    fun updateGoal(id: String, name: String, targetAmount: Double, deadline: String?) {
        viewModelScope.launch {
            when (val result = safeApiCall {
                goalsApi.updateGoal(id, UpdateGoalRequest(name, targetAmount, deadline))
            }) {
                is ApiResult.Success -> {
                    loadGoals()
                    _events.send(GoalEvent.Saved)
                }
                is ApiResult.Error -> _events.send(GoalEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(GoalEvent.Error("Network error."))
            }
        }
    }

    fun deleteGoal(id: String) {
        viewModelScope.launch {
            when (val result = safeApiCall { goalsApi.deleteGoal(id) }) {
                is ApiResult.Success -> {
                    loadGoals()
                    _events.send(GoalEvent.Deleted)
                }
                is ApiResult.Error -> _events.send(GoalEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(GoalEvent.Error("Network error."))
            }
        }
    }
}
