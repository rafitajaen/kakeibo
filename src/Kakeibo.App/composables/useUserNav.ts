import { computed } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { useAuthStore } from "@/stores/auth";

export function useUserNav() {
    const { locale } = useI18n();
    const router = useRouter();
    const auth = useAuthStore();

    const initials = computed(() => {
        const user = auth.user;
        if (!user) return "?";
        if (user.name) {
            return user.name
                .split(" ")
                .map((s: string) => s[0])
                .join("")
                .toUpperCase()
                .slice(0, 2);
        }
        return user.email[0].toUpperCase();
    });

    const displayName = computed(
        () => auth.user?.name ?? auth.user?.username ?? auth.user?.email ?? "",
    );

    function isDark(): boolean {
        return document.documentElement.classList.contains("dark");
    }

    function toggleTheme() {
        const html = document.documentElement;
        if (isDark()) {
            html.classList.remove("dark");
            localStorage.setItem("theme", "light");
        } else {
            html.classList.add("dark");
            localStorage.setItem("theme", "dark");
        }
    }

    function setLocale(lang: string) {
        locale.value = lang;
        localStorage.setItem("locale", lang);
    }

    async function handleLogout() {
        await auth.logout();
        router.push({ name: "login" });
    }

    return { initials, displayName, isDark, toggleTheme, setLocale, handleLogout, auth, locale };
}
