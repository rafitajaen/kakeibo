package com.kakeibo.features.goals.presentation

import com.kakeibo.features.goals.domain.Goal
import com.kakeibo.features.wallets.domain.Wallet

sealed interface GoalListUiState {
    data object Loading : GoalListUiState
    data class Success(val goals: List<Goal>) : GoalListUiState
    data class Error(val message: String) : GoalListUiState
}

sealed interface GoalDetailUiState {
    data object Loading : GoalDetailUiState
    data class Success(val goal: Goal) : GoalDetailUiState
    data class Error(val message: String) : GoalDetailUiState
}

sealed interface CreateGoalUiState {
    data object Idle : CreateGoalUiState
    data object Loading : CreateGoalUiState
    data class Ready(val wallets: List<Wallet>) : CreateGoalUiState
    data class Error(val message: String) : CreateGoalUiState
}
