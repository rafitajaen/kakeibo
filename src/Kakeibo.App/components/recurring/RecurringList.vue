<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { format, parseISO } from "date-fns";
import { Button } from "@/components/ui/button";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import FrequencyBadge from "@/components/recurring/FrequencyBadge.vue";
import type { RecurringPattern } from "@/stores/recurring";

defineProps<{
    patterns: RecurringPattern[];
}>();

const emit = defineEmits<{
    (e: "delete", id: string): void;
}>();

const { t } = useI18n();
const router = useRouter();

function handleEdit(id: string) {
    router.push({ name: "recurring-edit", params: { id } });
}

// Formats a YYYY-MM-DD date string for display (e.g. "Mar 15, 2026").
function formatDate(date: string): string {
    return format(parseISO(date), "PP");
}
</script>

<template>
    <div class="space-y-4">
        <div
            v-for="pattern in patterns"
            :key="pattern.id"
            class="border rounded-lg p-4 space-y-2"
            data-testid="pattern-item"
        >
            <div class="flex items-center justify-between">
                <div class="flex items-center gap-2">
                    <span class="font-medium">{{ pattern.name }}</span>
                    <FrequencyBadge :frequency="pattern.frequency" />
                </div>
                <div class="flex gap-2">
                    <Button variant="outline" size="sm" @click="handleEdit(pattern.id)">
                        {{ t("wallets.recurring.actions.edit") }}
                    </Button>

                    <!-- AlertDialog for delete confirmation -->
                    <AlertDialog>
                        <AlertDialogTrigger as-child>
                            <Button variant="destructive" size="sm">
                                {{ t("wallets.recurring.actions.delete") }}
                            </Button>
                        </AlertDialogTrigger>
                        <AlertDialogContent>
                            <AlertDialogHeader>
                                <AlertDialogTitle>
                                    {{ t("wallets.recurring.actions.deleteTitle") }}
                                </AlertDialogTitle>
                                <AlertDialogDescription>
                                    {{ t("wallets.recurring.actions.deleteConfirm") }}
                                </AlertDialogDescription>
                            </AlertDialogHeader>
                            <AlertDialogFooter>
                                <AlertDialogCancel>{{ t("common.cancel") }}</AlertDialogCancel>
                                <AlertDialogAction
                                    data-testid="delete-confirm"
                                    @click="emit('delete', pattern.id)"
                                >
                                    {{ t("wallets.recurring.actions.delete") }}
                                </AlertDialogAction>
                            </AlertDialogFooter>
                        </AlertDialogContent>
                    </AlertDialog>
                </div>
            </div>

            <div class="flex justify-between text-sm text-muted-foreground">
                <span>
                    {{ t("wallets.recurring.detail.amount") }}:
                    {{ pattern.amount.toFixed(2) }}
                    ({{
                        t(
                            `wallets.recurring.transactionType.${pattern.transactionType.toLowerCase()}`,
                        )
                    }})
                </span>
                <span>
                    {{ t("wallets.recurring.detail.nextOccurrence") }}:
                    {{ formatDate(pattern.nextOccurrence) }}
                </span>
            </div>

            <div class="text-xs text-muted-foreground">
                {{ t("wallets.recurring.detail.category") }}: {{ pattern.categoryName }}
                &middot;
                {{ t("wallets.recurring.detail.wallet") }}: {{ pattern.walletName }}
            </div>
        </div>
    </div>
</template>
