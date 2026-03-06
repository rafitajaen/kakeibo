<script setup lang="ts">
import { computed } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue-i18n";
import { SidebarTrigger } from "@/components/ui/sidebar";
import { Separator } from "@/components/ui/separator";
import {
    Breadcrumb,
    BreadcrumbItem,
    BreadcrumbLink,
    BreadcrumbList,
    BreadcrumbPage,
    BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { useWalletsStore } from "@/stores/wallets";
import { BREADCRUMB_MAP } from "@/lib/breadcrumbs";

const { t } = useI18n();
const route = useRoute();
const walletsStore = useWalletsStore();

interface ResolvedSegment {
    label: string;
    routeName?: string;
    routeParams?: Record<string, string>;
    isLast: boolean;
}

// Resolves the dynamic wallet name from the current route params.
function resolveWalletName(params: Record<string, string | string[]>): string {
    const id = params.id ?? params.walletId;
    if (!id) return "";
    const walletId = Array.isArray(id) ? id[0] : id;
    const wallet =
        walletsStore.wallets.find((w) => w.id === walletId) ?? walletsStore.currentWallet ?? null;
    return wallet?.name ?? walletId;
}

const segments = computed((): ResolvedSegment[] => {
    const name = route.name as string;
    const defs = BREADCRUMB_MAP[name];
    if (!defs || !defs.length) {
        return [{ label: t("app.name"), isLast: true }];
    }

    const params = route.params as Record<string, string | string[]>;
    const walletName = resolveWalletName(params);
    const walletId = Array.isArray(params.walletId)
        ? params.walletId[0]
        : (params.walletId ?? (Array.isArray(params.id) ? params.id[0] : params.id));

    return defs.map((seg, idx) => {
        const isLast = idx === defs.length - 1;
        const label = seg.dynamic ? walletName : t(seg.labelKey);

        // For the dynamic wallet segment, build correct route params
        let routeParams: Record<string, string> | undefined;
        if (seg.dynamic && seg.routeName) {
            routeParams = { id: walletId ?? "" };
        } else if (seg.routeName === "transactions" && walletId) {
            routeParams = { walletId };
        }

        return { label, routeName: isLast ? undefined : seg.routeName, routeParams, isLast };
    });
});
</script>

<template>
    <header
        class="flex h-12 shrink-0 items-center gap-2 border-b transition-[width,height] ease-linear group-has-[[data-collapsible=icon]]/sidebar-wrapper:h-12"
    >
        <div class="flex items-center gap-2 px-4">
            <SidebarTrigger class="-ml-1" />
            <Separator orientation="vertical" class="mr-2 h-4" />
            <Breadcrumb>
                <BreadcrumbList>
                    <template v-for="(seg, idx) in segments" :key="idx">
                        <BreadcrumbSeparator v-if="idx > 0" />
                        <BreadcrumbItem>
                            <BreadcrumbLink v-if="!seg.isLast && seg.routeName" :as-child="true">
                                <router-link
                                    :to="
                                        seg.routeParams
                                            ? { name: seg.routeName, params: seg.routeParams }
                                            : { name: seg.routeName }
                                    "
                                    >{{ seg.label }}</router-link
                                >
                            </BreadcrumbLink>
                            <BreadcrumbPage v-else>{{ seg.label }}</BreadcrumbPage>
                        </BreadcrumbItem>
                    </template>
                </BreadcrumbList>
            </Breadcrumb>
        </div>
    </header>
</template>
