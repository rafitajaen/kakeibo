<script setup lang="ts">
import type { Component } from "vue";
import { useRoute } from "vue-router";
import {
    SidebarGroup,
    SidebarGroupContent,
    SidebarMenu,
    SidebarMenuButton,
    SidebarMenuItem,
} from "@/components/ui/sidebar";

interface NavItem {
    title: string;
    routeName: string;
    icon: Component;
}

const props = defineProps<{
    items: NavItem[];
}>();

const route = useRoute();
</script>

<template>
    <SidebarGroup>
        <SidebarGroupContent>
            <SidebarMenu>
                <SidebarMenuItem v-for="item in props.items" :key="item.routeName">
                    <SidebarMenuButton as-child :is-active="route.name === item.routeName">
                        <router-link :to="{ name: item.routeName }">
                            <component :is="item.icon" class="size-4" />
                            <span>{{ item.title }}</span>
                        </router-link>
                    </SidebarMenuButton>
                </SidebarMenuItem>
            </SidebarMenu>
        </SidebarGroupContent>
    </SidebarGroup>
</template>
