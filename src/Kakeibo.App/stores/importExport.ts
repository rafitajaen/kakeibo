import { ref } from "vue";
import { defineStore } from "pinia";
import api from "@/lib/axios";

export type ImportSource = "kakeibo" | "onemoney";

export interface ImportCounts {
    wallets: number;
    categories: number;
    transactions: number;
    budgets: number;
    goals: number;
    recurringPatterns: number;
    settlements: number;
}

export interface ImportSkipped {
    sharedWalletMembers: number;
}

export interface ImportResult {
    importedAt: string;
    source: string;
    format: string;
    counts: ImportCounts;
    skipped: ImportSkipped;
}

export const useImportExportStore = defineStore("importExport", () => {
    const isExporting = ref(false);
    const exportError = ref<string | null>(null);

    const isImporting = ref(false);
    const importResult = ref<ImportResult | null>(null);
    const importError = ref<string | null>(null);

    // Downloads user data as a SQLite or CSV ZIP file by triggering a browser download.
    async function exportData(format: "sqlite" | "csv"): Promise<void> {
        isExporting.value = true;
        exportError.value = null;
        try {
            const response = await api.get(`/api/users/me/export?format=${format}`, {
                responseType: "blob",
            });
            const filename = format === "sqlite" ? "kakeibo-export.db" : "kakeibo-export.zip";
            const mimeType = format === "sqlite" ? "application/x-sqlite3" : "application/zip";
            const url = URL.createObjectURL(new Blob([response.data], { type: mimeType }));
            const link = document.createElement("a");
            link.href = url;
            link.download = filename;
            document.body.appendChild(link);
            link.click();
            link.remove();
            URL.revokeObjectURL(url);
        } catch {
            exportError.value = "export_failed";
        } finally {
            isExporting.value = false;
        }
    }

    // Uploads a backup file and imports financial data for the authenticated user.
    async function importData(file: File, source: ImportSource): Promise<void> {
        isImporting.value = true;
        importResult.value = null;
        importError.value = null;
        try {
            const formData = new FormData();
            formData.append("file", file);
            formData.append("source", source);
            const response = await api.post<ImportResult>("/api/users/me/import", formData, {
                headers: { "Content-Type": "multipart/form-data" },
            });
            importResult.value = response.data;
        } catch {
            importError.value = "import_failed";
        } finally {
            isImporting.value = false;
        }
    }

    function clearImportResult(): void {
        importResult.value = null;
        importError.value = null;
    }

    return {
        isExporting,
        exportError,
        isImporting,
        importResult,
        importError,
        exportData,
        importData,
        clearImportResult,
    };
});
