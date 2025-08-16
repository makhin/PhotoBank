const isDev =
  // node / jest / vite build
  (typeof process !== 'undefined' && process.env?.NODE_ENV !== 'production') ||
  // browser vite
  (typeof import.meta !== 'undefined' && import.meta.env?.DEV === true);
export const logger = {
  debug: (...a: unknown[]) => { if (isDev) console.debug(...a); },
  warn:  (...a: unknown[]) => { if (isDev) console.warn(...a);  },
  error: (...a: unknown[]) => { if (isDev) console.error(...a); },
} as const;
