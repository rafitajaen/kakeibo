<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import api from "@/lib/axios";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import ProgressIndicator from "@/components/onboarding/ProgressIndicator.vue";
import WelcomeStep from "@/components/onboarding/WelcomeStep.vue";
import TourStep from "@/components/onboarding/TourStep.vue";
import WalletSetupStep from "@/components/onboarding/WalletSetupStep.vue";
import CompletionStep from "@/components/onboarding/CompletionStep.vue";

const router = useRouter();
const { t } = useI18n();

const TOTAL_STEPS = 5;
const currentStep = ref(0);
const isSeedLoading = ref(false);

function nextStep() {
    if (currentStep.value < TOTAL_STEPS - 1) {
        currentStep.value++;
    }
}

function prevStep() {
    if (currentStep.value > 0) {
        currentStep.value--;
    }
}

function skip() {
    router.replace({ name: "home" });
}

function finish() {
    router.replace({ name: "home" });
}

async function loadSeedData() {
    isSeedLoading.value = true;
    try {
        await api.post("/api/users/me/seed-data");
    } finally {
        isSeedLoading.value = false;
        router.replace({ name: "home" });
    }
}
</script>

<template>
    <div class="min-h-screen flex items-center justify-center bg-background p-4">
        <Card class="w-full max-w-lg">
            <CardContent class="p-8">
                <div class="space-y-6">
                    <ProgressIndicator :current-step="currentStep" :total-steps="TOTAL_STEPS" />
                    <WelcomeStep v-if="currentStep === 0" @next="nextStep" @skip="skip" />
                    <TourStep v-else-if="currentStep === 1" @next="nextStep" @back="prevStep" />
                    <WalletSetupStep
                        v-else-if="currentStep === 2"
                        @next="nextStep"
                        @back="prevStep"
                        @skip="nextStep"
                    />
                    <CompletionStep v-else-if="currentStep === 3" @finish="nextStep" />

                    <!-- Seed data step -->
                    <div v-else-if="currentStep === 4" class="space-y-4 text-center">
                        <h2 class="text-xl font-semibold">{{ t("onboarding.seedData.title") }}</h2>
                        <p class="text-muted-foreground text-sm">
                            {{ t("onboarding.seedData.description") }}
                        </p>
                        <div class="flex flex-col gap-2">
                            <Button :disabled="isSeedLoading" @click="loadSeedData">
                                {{
                                    isSeedLoading
                                        ? t("common.loading")
                                        : t("onboarding.seedData.yes")
                                }}
                            </Button>
                            <Button variant="ghost" @click="finish">
                                {{ t("onboarding.seedData.no") }}
                            </Button>
                        </div>
                    </div>
                </div>
            </CardContent>
        </Card>
    </div>
</template>
