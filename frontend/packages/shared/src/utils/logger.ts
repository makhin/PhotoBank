const isDev = typeof import.meta !== 'undefined' && import.meta.env && import.meta.env.DEV;
export const logger = {
  debug: (...a: unknown[]) => { if (isDev) console.debug(...a); },
  warn:  (...a: unknown[]) => { if (isDev) console.warn(...a);  },
  error: (...a: unknown[]) => { if (isDev) console.error(...a); },
} as const;
