import { computed } from "vue";
import { useAuthStore } from "@/stores/auth";

/**
 * Returns a `formatCurrency(amount)` function that respects the user's
 * display preferences (decimal separator, group separator, symbol position,
 * and display mode) stored in the auth store.
 *
 * Falls back to standard Intl formatting when preferences are not loaded yet.
 */
export function useCurrencyFormat() {
    const authStore = useAuthStore();

    const formatCurrency = computed(() => {
        return (amount: number): string => {
            const user = authStore.user;
            if (!user) {
                return amount.toFixed(2);
            }

            const {
                currency,
                currencyDecimalSeparator,
                currencyGroupSeparator,
                currencySymbolPosition,
                currencyDisplay,
            } = user;

            // Use Intl.NumberFormat to get the base formatted number (no symbol).
            const formatter = new Intl.NumberFormat(undefined, {
                minimumFractionDigits: 2,
                maximumFractionDigits: 2,
                useGrouping: currencyGroupSeparator !== "",
            });

            // Format the raw number, then replace Intl's separators with user's preferences.
            // Intl uses "." for decimal and "," for groups in en-US locale by default.
            // We replace them to match the user's chosen separators.
            let formatted = formatter.format(amount);

            // Replace Intl decimal/group separators with user preferences.
            // We use an intermediate placeholder to avoid double-replacement.
            const intlDecimal =
                new Intl.NumberFormat(undefined, { minimumFractionDigits: 1 })
                    .formatToParts(1.1)
                    .find((p) => p.type === "decimal")?.value ?? ".";
            const intlGroup =
                new Intl.NumberFormat(undefined, { useGrouping: true })
                    .formatToParts(1000)
                    .find((p) => p.type === "group")?.value ?? ",";

            if (intlGroup !== currencyGroupSeparator && currencyGroupSeparator !== "") {
                formatted = formatted.split(intlGroup).join("GRPSEP");
            }
            if (intlDecimal !== currencyDecimalSeparator) {
                formatted = formatted.split(intlDecimal).join(currencyDecimalSeparator);
            }
            if (intlGroup !== currencyGroupSeparator && currencyGroupSeparator !== "") {
                formatted = formatted.split("GRPSEP").join(currencyGroupSeparator);
            } else if (currencyGroupSeparator === "") {
                // Remove group separators entirely
                formatted = formatted.split(intlGroup).join("");
            }

            // Build the symbol/code string
            let symbol = "";
            if (currencyDisplay === "symbol") {
                // Get the currency symbol for the user's currency.
                const parts = new Intl.NumberFormat(undefined, {
                    style: "currency",
                    currency,
                    currencyDisplay: "symbol",
                }).formatToParts(0);
                symbol = parts.find((p) => p.type === "currency")?.value ?? currency;
            } else if (currencyDisplay === "code") {
                symbol = currency;
            }

            if (!symbol) return formatted;

            return currencySymbolPosition === "before"
                ? `${symbol}${formatted}`
                : `${formatted} ${symbol}`;
        };
    });

    return { formatCurrency };
}
