<script setup lang="ts">
import { ref, watch } from "vue";
import { Input } from "@/components/ui/input";

const props = defineProps<{
    modelValue: number;
}>();

const emit = defineEmits<{
    (e: "update:modelValue", value: number): void;
}>();

// Internal display string — may contain arithmetic expressions like "100 + 50".
const displayValue = ref(props.modelValue === 0 ? "" : props.modelValue.toString());

watch(
    () => props.modelValue,
    (val) => {
        displayValue.value = val === 0 ? "" : val.toString();
    },
);

// Evaluates a mathematical expression using only digits, decimals, and basic operators.
// Returns null if the expression is invalid or evaluates to a non-positive number.
function evaluateExpression(expr: string): number | null {
    const sanitized = expr.replace(/\s/g, "");
    if (!sanitized) return null;
    // Only allow digits, decimal points, and the four basic arithmetic operators + parentheses
    if (!/^[\d.+\-*/()]+$/.test(sanitized)) return null;
    try {
        // oxlint-disable-next-line no-new-func
        const result = Function('"use strict"; return (' + sanitized + ")")() as number;
        if (typeof result !== "number" || !isFinite(result) || result <= 0) return null;
        return Math.round(result * 100) / 100;
    } catch {
        return null;
    }
}

// Commits the current display value: evaluates expression, emits result, or reverts.
function handleCommit() {
    const result = evaluateExpression(displayValue.value);
    if (result !== null) {
        emit("update:modelValue", result);
        displayValue.value = result.toString();
    } else {
        // Invalid expression or zero — revert to the last valid modelValue
        displayValue.value = props.modelValue === 0 ? "" : props.modelValue.toString();
    }
}
</script>

<template>
    <Input
        type="text"
        v-model="displayValue"
        @blur="handleCommit"
        @keydown.enter.prevent="handleCommit"
    />
</template>
