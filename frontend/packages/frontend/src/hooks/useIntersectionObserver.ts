import { useEffect, type RefObject } from 'react';

interface UseIntersectionObserverOptions {
  target: RefObject<Element | null>;
  onIntersect: () => void;
  enabled?: boolean;
  root?: RefObject<Element | null>;
  rootMargin?: string;
  threshold?: number | number[];
}

export const useIntersectionObserver = ({
  target,
  onIntersect,
  enabled = true,
  root,
  rootMargin,
  threshold,
}: UseIntersectionObserverOptions) => {
  useEffect(() => {
    if (!enabled) {
      return;
    }

    const element = target.current;
    const rootElement = root?.current ?? undefined;

    if (!element) {
      return;
    }

    if (root && !rootElement) {
      return;
    }

    if (typeof IntersectionObserver === 'undefined') {
      return;
    }

    const observer = new IntersectionObserver(
      (entries) => {
        if (!enabled) {
          return;
        }

        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            onIntersect();
          }
        });
      },
      {
        root: rootElement,
        rootMargin,
        threshold,
      }
    );

    observer.observe(element);

    return () => {
      observer.disconnect();
    };
  }, [enabled, onIntersect, root, rootMargin, target, threshold]);
};
