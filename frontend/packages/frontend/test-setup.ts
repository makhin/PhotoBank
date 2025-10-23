import '@testing-library/jest-dom/vitest';
import './src/shared/config/i18n';
import { afterAll } from 'vitest';

const suppressedConsoleWarnings = [
  'Missing `Description` or `aria-describedby={undefined}` for {DialogContent}.',
];

const shouldSuppressConsoleMessage = (message: unknown) =>
  typeof message === 'string' &&
  suppressedConsoleWarnings.some((warning) => message.includes(warning));

const interceptConsole = <T extends (...args: unknown[]) => void>(original: T) =>
  (...args: Parameters<T>): void => {
    if (shouldSuppressConsoleMessage(args[0])) {
      return;
    }

    original.apply(console, args);
  };

const originalConsoleError = console.error;
const originalConsoleWarn = console.warn;

console.error = interceptConsole(originalConsoleError);
console.warn = interceptConsole(originalConsoleWarn);

afterAll(() => {
  console.error = originalConsoleError;
  console.warn = originalConsoleWarn;
});

class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
}

(globalThis as unknown as { ResizeObserver: typeof ResizeObserver }).ResizeObserver = ResizeObserver;

if (typeof window !== 'undefined') {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: (query: string) => ({
      media: query,
      matches: false,
      onchange: null,
      addListener() {},
      removeListener() {},
      addEventListener() {},
      removeEventListener() {},
      dispatchEvent() {
        return false;
      },
    }),
  });
}
