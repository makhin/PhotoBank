const env = typeof __APP_ENV__ !== 'undefined' ? __APP_ENV__ : (import.meta?.env ?? {});

export { env };
