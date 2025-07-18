// Determine API base URL from environment variables for both Node and browser
// contexts. Vite exposes variables prefixed with `VITE_` to the client.
export const API_BASE_URL: string = (() => {
    // Vite during build replaces `import.meta.env` with the actual environment
    // values. Use `VITE_API_BASE_URL` if present.
    if (typeof import.meta !== 'undefined' && (import.meta as any).env?.VITE_API_BASE_URL) {
        return (import.meta as any).env.VITE_API_BASE_URL as string;
    }

    // When running under Node (e.g. in the Telegram bot) fall back to standard
    // environment variables.
    if (typeof process !== 'undefined') {
        if (process.env.VITE_API_BASE_URL) {
            return process.env.VITE_API_BASE_URL;
        }
        if (process.env.API_BASE_URL) {
            return process.env.API_BASE_URL;
        }
    }

    // Default value used in development when no environment variable is set.
    return "http://localhost:5066";
})();

export function isBrowser(): boolean {
    return typeof window !== 'undefined' && typeof window.crypto !== 'undefined';
}