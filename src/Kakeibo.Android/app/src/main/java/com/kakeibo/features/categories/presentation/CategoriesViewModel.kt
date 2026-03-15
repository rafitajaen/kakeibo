package com.kakeibo.features.categories.presentation

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.kakeibo.core.api.ApiResult
import com.kakeibo.core.api.safeApiCall
import com.kakeibo.features.categories.data.CategoriesApi
import com.kakeibo.features.categories.data.CreateCategoryRequest
import com.kakeibo.features.categories.data.UpdateCategoryRequest
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed interface CategoryEvent {
    data object Saved : CategoryEvent
    data object Deleted : CategoryEvent
    data class Error(val message: String) : CategoryEvent
}

@HiltViewModel
class CategoriesViewModel @Inject constructor(
    private val categoriesApi: CategoriesApi,
) : ViewModel() {

    private val _uiState = MutableStateFlow<CategoryListUiState>(CategoryListUiState.Loading)
    val uiState = _uiState.asStateFlow()

    private val _events = Channel<CategoryEvent>()
    val events = _events.receiveAsFlow()

    init {
        loadCategories()
    }

    fun loadCategories() {
        viewModelScope.launch {
            _uiState.value = CategoryListUiState.Loading
            when (val result = safeApiCall { categoriesApi.getCategories() }) {
                is ApiResult.Success -> _uiState.value = CategoryListUiState.Success(result.data)
                is ApiResult.Error -> _uiState.value = CategoryListUiState.Error(result.message)
                is ApiResult.NetworkError -> _uiState.value = CategoryListUiState.Error("Network error.")
            }
        }
    }

    fun createCategory(name: String, color: String?, isPrivate: Boolean) {
        viewModelScope.launch {
            when (val result = safeApiCall {
                categoriesApi.createCategory(CreateCategoryRequest(name, color, isPrivate))
            }) {
                is ApiResult.Success -> {
                    loadCategories()
                    _events.send(CategoryEvent.Saved)
                }
                is ApiResult.Error -> _events.send(CategoryEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(CategoryEvent.Error("Network error."))
            }
        }
    }

    fun updateCategory(id: String, name: String, color: String?, isPrivate: Boolean) {
        viewModelScope.launch {
            when (val result = safeApiCall {
                categoriesApi.updateCategory(id, UpdateCategoryRequest(name, color, isPrivate))
            }) {
                is ApiResult.Success -> {
                    loadCategories()
                    _events.send(CategoryEvent.Saved)
                }
                is ApiResult.Error -> _events.send(CategoryEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(CategoryEvent.Error("Network error."))
            }
        }
    }

    fun deleteCategory(id: String) {
        viewModelScope.launch {
            when (val result = safeApiCall { categoriesApi.deleteCategory(id) }) {
                is ApiResult.Success -> {
                    loadCategories()
                    _events.send(CategoryEvent.Deleted)
                }
                is ApiResult.Error -> _events.send(CategoryEvent.Error(result.message))
                is ApiResult.NetworkError -> _events.send(CategoryEvent.Error("Network error."))
            }
        }
    }
}
