<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { Download, Paperclip, Trash2 } from "lucide-vue-next";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import type { TransactionAttachment } from "@/stores/transactions";

const props = defineProps<{
    transactionId: string;
    attachments: TransactionAttachment[];
    isLoading: boolean;
}>();

const emit = defineEmits<{
    (e: "upload", file: File): void;
    (e: "download", attachment: TransactionAttachment): void;
    (e: "delete", attachmentId: string): void;
}>();

const { t } = useI18n();

// Allowed MIME types — mirrors backend validation.
const ALLOWED_TYPES = ["application/pdf", "image/jpeg", "image/png", "image/webp"];

// Formats bytes into a human-readable size string (e.g. 1.2 MB).
function formatFileSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function handleFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    if (!ALLOWED_TYPES.includes(file.type)) {
        alert(t("transactions.attachments.allowedTypes"));
        input.value = "";
        return;
    }

    emit("upload", file);
    // Reset input so the same file can be re-selected after deletion.
    input.value = "";
}

function triggerFileInput(): void {
    document.getElementById(`attachment-input-${props.transactionId}`)?.click();
}
</script>

<template>
    <div class="space-y-3">
        <div class="flex items-center justify-between">
            <h3 class="text-sm font-medium">{{ t("transactions.attachments.title") }}</h3>
            <Button type="button" variant="outline" size="sm" @click="triggerFileInput">
                <Paperclip class="mr-2 h-4 w-4" />
                {{ t("transactions.attachments.add") }}
            </Button>
        </div>

        <!-- Hidden file input -->
        <input
            :id="`attachment-input-${transactionId}`"
            type="file"
            class="hidden"
            accept=".pdf,.jpg,.jpeg,.png,.webp"
            @change="handleFileChange"
        />

        <!-- Loading skeleton -->
        <template v-if="isLoading">
            <Skeleton class="h-10 w-full" />
            <Skeleton class="h-10 w-full" />
        </template>

        <!-- Empty state -->
        <p v-else-if="attachments.length === 0" class="text-sm text-muted-foreground">
            {{ t("transactions.attachments.empty") }}
        </p>

        <!-- Attachment list -->
        <ul v-else class="divide-y divide-border rounded-md border">
            <li
                v-for="attachment in attachments"
                :key="attachment.id"
                class="flex items-center justify-between px-3 py-2"
            >
                <div class="min-w-0 flex-1 truncate">
                    <span class="truncate text-sm font-medium">{{ attachment.fileName }}</span>
                    <span class="ml-2 text-xs text-muted-foreground">
                        {{ formatFileSize(attachment.fileSizeBytes) }}
                    </span>
                </div>
                <div class="ml-2 flex shrink-0 gap-1">
                    <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        :title="t('transactions.attachments.download')"
                        @click="emit('download', attachment)"
                    >
                        <Download class="h-4 w-4" />
                    </Button>
                    <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        :title="t('transactions.attachments.delete')"
                        @click="emit('delete', attachment.id)"
                    >
                        <Trash2 class="h-4 w-4 text-destructive" />
                    </Button>
                </div>
            </li>
        </ul>
    </div>
</template>
