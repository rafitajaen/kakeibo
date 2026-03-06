<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { useWalletsStore, type Wallet } from "@/stores/wallets";
import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from "@/components/ui/dialog";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import WalletForm from "@/components/wallets/WalletForm.vue";

const props = defineProps<{
    wallet: Wallet;
}>();

const emit = defineEmits<{
    (e: "updated"): void;
    (e: "archived"): void;
}>();

const { t } = useI18n();
const router = useRouter();
const walletsStore = useWalletsStore();

const open = ref(false);
const isSubmitting = ref(false);
const showArchiveConfirm = ref(false);
const apiError = ref<string | null>(null);

async function handleSubmit(values: {
    name: string;
    icon?: string | null;
    backgroundColor?: string | null;
    textColor?: string | null;
}) {
    isSubmitting.value = true;
    apiError.value = null;
    try {
        await walletsStore.updateWallet(props.wallet.id, {
            name: values.name,
            icon: values.icon,
            backgroundColor: values.backgroundColor,
            textColor: values.textColor,
        });
        open.value = false;
        emit("updated");
    } catch (err: unknown) {
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 409) {
            apiError.value = t("wallets.errors.conflict");
        } else {
            apiError.value = t("wallets.errors.unexpected");
        }
    } finally {
        isSubmitting.value = false;
    }
}

async function handleArchive() {
    showArchiveConfirm.value = false;
    try {
        await walletsStore.archiveWallet(props.wallet.id);
        open.value = false;
        emit("archived");
        router.push({ name: "wallets" });
    } catch {
        apiError.value = t("wallets.errors.unexpected");
    }
}
</script>

<template>
    <Dialog v-model:open="open">
        <DialogTrigger as-child>
            <slot />
        </DialogTrigger>
        <DialogContent class="sm:max-w-md">
            <DialogHeader>
                <DialogTitle>{{ t("wallets.actions.edit") }}</DialogTitle>
            </DialogHeader>

            <WalletForm
                :initial-name="wallet.name"
                :initial-icon="wallet.icon"
                :initial-background-color="wallet.backgroundColor"
                :initial-text-color="wallet.textColor"
                :is-submitting="isSubmitting"
                @submit="handleSubmit"
            />

            <p v-if="apiError" class="text-sm text-destructive">{{ apiError }}</p>

            <!-- Danger zone -->
            <div class="border-t pt-4 mt-2">
                <p class="text-sm font-medium text-destructive mb-2">
                    {{ t("wallets.dangerZone") }}
                </p>
                <Button
                    variant="outline"
                    class="w-full text-destructive hover:text-destructive border-destructive/50"
                    type="button"
                    @click="showArchiveConfirm = true"
                >
                    {{ t("wallets.actions.archive") }}
                </Button>
            </div>
        </DialogContent>
    </Dialog>

    <!-- Archive confirmation dialog -->
    <AlertDialog v-model:open="showArchiveConfirm">
        <AlertDialogContent>
            <AlertDialogHeader>
                <AlertDialogTitle>{{ t("wallets.actions.archive") }}</AlertDialogTitle>
                <AlertDialogDescription>{{
                    t("wallets.actions.archiveConfirm")
                }}</AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
                <AlertDialogCancel>{{ t("common.cancel") }}</AlertDialogCancel>
                <AlertDialogAction
                    class="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                    @click="handleArchive"
                >
                    {{ t("wallets.actions.archive") }}
                </AlertDialogAction>
            </AlertDialogFooter>
        </AlertDialogContent>
    </AlertDialog>
</template>
