<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";
import { Card, CardContent } from "@/components/ui/card";
import ProgressIndicator from "@/components/onboarding/ProgressIndicator.vue";
import WelcomeStep from "@/components/onboarding/WelcomeStep.vue";
import TourStep from "@/components/onboarding/TourStep.vue";
import WalletSetupStep from "@/components/onboarding/WalletSetupStep.vue";
import CompletionStep from "@/components/onboarding/CompletionStep.vue";

const router = useRouter();

const TOTAL_STEPS = 4;
const currentStep = ref(0);

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
                    <CompletionStep v-else-if="currentStep === 3" @finish="finish" />
                </div>
            </CardContent>
        </Card>
    </div>
</template>
