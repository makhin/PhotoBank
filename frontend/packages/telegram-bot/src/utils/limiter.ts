import Bottleneck from 'bottleneck';

// Глобальный лимитер: не более 1 send в 200ms (примерно)
export const limiter = new Bottleneck({
  minTime: 200, // 5 ops/sec
  maxConcurrent: 1, // последовательные отправки
});

// Обёртка для вызовов Telegram API
export async function throttled<T>(fn: () => Promise<T>): Promise<T> {
  return limiter.schedule(fn);
}
