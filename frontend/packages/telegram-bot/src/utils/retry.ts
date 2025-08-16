export async function withTelegramRetry<T>(fn: () => Promise<T>, maxAttempts = 3): Promise<T> {
  let attempt = 0;
  for (;;) {
    try {
      return await fn();
      } catch (e: unknown) {
        attempt++;
        const err = e as { error_code?: number; description?: string };
        const code = err.error_code;
        const desc = err.description ?? '';
      if (code === 429) {
        const match = /retry after (\d+)/i.exec(desc);
        const wait = match ? Number(match[1]) * 1000 : Math.min(2000 * attempt, 8000);
        await new Promise(r => setTimeout(r, wait));
        if (attempt < maxAttempts) continue;
      }
      throw e;
    }
  }
}
