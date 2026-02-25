<script setup lang="ts">
import { AlertDialogRoot, useForwardPropsEmits } from "reka-ui";

// Props inlined to avoid Omit<DialogRootProps, 'modal'> which Vue's SFC compiler cannot resolve
const props = defineProps<{
    open?: boolean;
    defaultOpen?: boolean;
}>();

const emits = defineEmits<{
    "update:open": [value: boolean];
}>();

const forwarded = useForwardPropsEmits(props, emits);
</script>

<template>
    <AlertDialogRoot v-bind="forwarded">
        <slot />
    </AlertDialogRoot>
</template>
