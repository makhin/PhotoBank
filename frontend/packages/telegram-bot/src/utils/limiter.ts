import Bottleneck from 'bottleneck';

// Global limiter: no more than 1 send every 200ms
export const limiter = new Bottleneck({
  minTime: 200, // 5 ops/sec
  maxConcurrent: 1, // sequential sends
});

// Wrapper for Telegram API calls
export function throttled<T>(fn: () => Promise<T>): Promise<T> {
  return limiter.schedule(fn);
}
