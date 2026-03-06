<script setup lang="ts">
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { Download, Upload, FileUp } from "lucide-vue-next";
import { Button } from "@/components/ui/button";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Separator } from "@/components/ui/separator";
import { useImportExportStore, type ImportSource } from "@/stores/importExport";

const { t } = useI18n();
const store = useImportExportStore();

// --- Export ---
const exportingFormat = ref<"sqlite" | "csv" | null>(null);

async function handleExport(format: "sqlite" | "csv") {
    exportingFormat.value = format;
    await store.exportData(format);
    exportingFormat.value = null;
}

// --- Import ---
const selectedSource = ref<ImportSource>("kakeibo");
const selectedFile = ref<File | null>(null);
const fileInputRef = ref<HTMLInputElement | null>(null);

const acceptedExtensions = computed(() => {
    return selectedSource.value === "onemoney" ? ".backup,.db,.sqlite" : ".db,.zip";
});

function onFileChange(event: Event) {
    const input = event.target as HTMLInputElement;
    selectedFile.value = input.files?.[0] ?? null;
}

function triggerFileInput() {
    fileInputRef.value?.click();
}

function onSourceChange() {
    // Clear file selection when source changes since accepted types differ
    selectedFile.value = null;
    if (fileInputRef.value) fileInputRef.value.value = "";
    store.clearImportResult();
}

async function handleImport() {
    if (!selectedFile.value) return;
    await store.importData(selectedFile.value, selectedSource.value);
}
</script>

<template>
    <div class="space-y-6">
        <!-- Export Card -->
        <div class="space-y-3">
            <div>
                <h3 class="text-sm font-medium">{{ t("settings.importExport.exportTitle") }}</h3>
                <p class="text-sm text-muted-foreground mt-1">
                    {{ t("settings.importExport.exportDescription") }}
                </p>
            </div>
            <div class="flex flex-wrap gap-2">
                <Button
                    variant="outline"
                    size="sm"
                    :disabled="store.isExporting"
                    @click="handleExport('sqlite')"
                >
                    <Download class="mr-2 h-4 w-4" />
                    <span v-if="exportingFormat === 'sqlite'">{{ t("common.loading") }}</span>
                    <span v-else>{{ t("settings.importExport.exportSqlite") }}</span>
                </Button>
                <Button
                    variant="outline"
                    size="sm"
                    :disabled="store.isExporting"
                    @click="handleExport('csv')"
                >
                    <Download class="mr-2 h-4 w-4" />
                    <span v-if="exportingFormat === 'csv'">{{ t("common.loading") }}</span>
                    <span v-else>{{ t("settings.importExport.exportCsv") }}</span>
                </Button>
            </div>
            <Alert v-if="store.exportError" variant="destructive">
                <AlertDescription>{{ t("settings.importExport.error") }}</AlertDescription>
            </Alert>
        </div>

        <Separator />

        <!-- Import Card -->
        <div class="space-y-3">
            <div>
                <h3 class="text-sm font-medium">{{ t("settings.importExport.importTitle") }}</h3>
                <p class="text-sm text-muted-foreground mt-1">
                    {{ t("settings.importExport.importDescription") }}
                </p>
            </div>

            <!-- Source selector -->
            <div class="space-y-1">
                <label class="text-sm font-medium">{{
                    t("settings.importExport.importSource")
                }}</label>
                <Select v-model="selectedSource" @update:model-value="onSourceChange">
                    <SelectTrigger class="w-52">
                        <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                        <SelectItem value="kakeibo">
                            {{ t("settings.importExport.importSourceKakeibo") }}
                        </SelectItem>
                        <SelectItem value="onemoney">
                            {{ t("settings.importExport.importSourceOnemoney") }}
                        </SelectItem>
                    </SelectContent>
                </Select>
            </div>

            <!-- File picker -->
            <div class="flex items-center gap-2">
                <input
                    ref="fileInputRef"
                    type="file"
                    class="hidden"
                    :accept="acceptedExtensions"
                    @change="onFileChange"
                />
                <Button variant="outline" size="sm" @click="triggerFileInput">
                    <FileUp class="mr-2 h-4 w-4" />
                    {{ t("settings.importExport.importFile") }}
                </Button>
                <span v-if="selectedFile" class="text-sm text-muted-foreground truncate max-w-xs">
                    {{ selectedFile.name }}
                </span>
            </div>

            <Button size="sm" :disabled="!selectedFile || store.isImporting" @click="handleImport">
                <Upload class="mr-2 h-4 w-4" />
                <span v-if="store.isImporting">{{ t("common.loading") }}</span>
                <span v-else>{{ t("settings.importExport.importButton") }}</span>
            </Button>

            <!-- Import result table -->
            <div v-if="store.importResult" class="space-y-3">
                <p class="text-sm font-medium text-green-600 dark:text-green-400">
                    {{ t("settings.importExport.importSuccess") }}
                </p>
                <div class="rounded-md border overflow-hidden">
                    <table class="w-full text-sm">
                        <tbody>
                            <tr class="border-b last:border-0">
                                <td class="px-3 py-2 font-medium text-muted-foreground">
                                    {{ t("settings.importExport.importSource") }}
                                </td>
                                <td class="px-3 py-2">
                                    {{ store.importResult.source }} ({{
                                        store.importResult.format
                                    }})
                                </td>
                            </tr>
                            <tr class="border-b last:border-0 bg-muted/30">
                                <td class="px-3 py-2 font-medium text-muted-foreground">Wallets</td>
                                <td class="px-3 py-2">{{ store.importResult.counts.wallets }}</td>
                            </tr>
                            <tr class="border-b last:border-0">
                                <td class="px-3 py-2 font-medium text-muted-foreground">
                                    Categories
                                </td>
                                <td class="px-3 py-2">
                                    {{ store.importResult.counts.categories }}
                                </td>
                            </tr>
                            <tr class="border-b last:border-0 bg-muted/30">
                                <td class="px-3 py-2 font-medium text-muted-foreground">
                                    Transactions
                                </td>
                                <td class="px-3 py-2">
                                    {{ store.importResult.counts.transactions }}
                                </td>
                            </tr>
                            <tr
                                v-if="store.importResult.counts.budgets > 0"
                                class="border-b last:border-0"
                            >
                                <td class="px-3 py-2 font-medium text-muted-foreground">Budgets</td>
                                <td class="px-3 py-2">{{ store.importResult.counts.budgets }}</td>
                            </tr>
                            <tr
                                v-if="store.importResult.counts.goals > 0"
                                class="border-b last:border-0 bg-muted/30"
                            >
                                <td class="px-3 py-2 font-medium text-muted-foreground">Goals</td>
                                <td class="px-3 py-2">{{ store.importResult.counts.goals }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>

                <Alert v-if="store.importResult.skipped.sharedWalletMembers > 0">
                    <AlertDescription>
                        {{
                            t("settings.importExport.importSkippedMembers", {
                                count: store.importResult.skipped.sharedWalletMembers,
                            })
                        }}
                    </AlertDescription>
                </Alert>
            </div>

            <!-- Import error -->
            <Alert v-if="store.importError" variant="destructive">
                <AlertDescription>{{ t("settings.importExport.error") }}</AlertDescription>
            </Alert>
        </div>
    </div>
</template>
