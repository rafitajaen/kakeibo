/**
 * Extracts the HTTP status code from an unknown caught error (axios error shape).
 * Returns null when the error has no response (e.g. network timeout).
 */
export function getHttpStatus(error: unknown): number | null {
    return (error as { response?: { status?: number } })?.response?.status ?? null;
}
