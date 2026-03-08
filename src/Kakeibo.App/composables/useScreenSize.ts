import { ref, onMounted, onUnmounted } from "vue";

export function useScreenSize() {
    const isMobile = ref(
        typeof window !== "undefined" ? !window.matchMedia("(min-width: 768px)").matches : true,
    );
    let mq: MediaQueryList | null = null;

    function handler(e: MediaQueryListEvent) {
        isMobile.value = !e.matches;
    }

    onMounted(() => {
        mq = window.matchMedia("(min-width: 768px)");
        isMobile.value = !mq.matches;
        mq.addEventListener("change", handler);
    });

    onUnmounted(() => {
        mq?.removeEventListener("change", handler);
    });

    return { isMobile };
}
