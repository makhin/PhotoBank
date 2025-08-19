import { useEffect, useRef, useState } from 'react';

export function useInView<T extends Element>(
  opts: IntersectionObserverInit = {
    rootMargin: '400px 0px',
    threshold: 0.01,
  }
): [React.RefObject<T | null>, boolean] {
  const ref = useRef<T | null>(null);
  const [inView, setInView] = useState(false);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;

    const observer = new IntersectionObserver((entries) => {
      if (entries[0]) {
        setInView(entries[0].isIntersecting);
      }
    }, opts);

    observer.observe(el);
    return () => observer.disconnect();
  }, [opts]);

  return [ref, inView];
}
