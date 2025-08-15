import { useCallback } from 'react';

export function prefetch(src: string): Promise<void> {
  return new Promise((resolve) => {
    const img = new Image();
    img.onload = img.onerror = () => resolve();
    img.src = src;
  });
}

export function usePrefetchOnHover(srcs: string[]) {
  const handler = useCallback(() => {
    srcs.forEach((s) => {
      if (s) prefetch(s);
    });
  }, [srcs]);

  return {
    onMouseEnter: handler,
    onFocus: handler,
  } as const;
}
