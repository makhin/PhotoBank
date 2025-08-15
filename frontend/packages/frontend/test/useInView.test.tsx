import { render, screen, act } from '@testing-library/react';
import { useInView } from '@/hooks/useInView';
import { vi } from 'vitest';
import React from 'react';

describe('useInView', () => {
  it('updates when element enters view', () => {
    let cb: (entries: IntersectionObserverEntry[]) => void = () => {};
    const observe = vi.fn();
    const disconnect = vi.fn();
    const Original = (globalThis as any).IntersectionObserver;
    class IO {
      constructor(callback: any) {
        cb = callback;
      }
      observe = observe;
      disconnect = disconnect;
    }
    // @ts-ignore
    (globalThis as any).IntersectionObserver = IO;

    const Test = () => {
      const [ref, inView] = useInView<HTMLDivElement>();
      return <div ref={ref}>{inView ? 'in' : 'out'}</div>;
    };

    render(<Test />);
    expect(screen.getByText('out')).toBeInTheDocument();
    act(() => {
      cb([{ isIntersecting: true } as IntersectionObserverEntry]);
    });
    expect(screen.getByText('in')).toBeInTheDocument();

    (globalThis as any).IntersectionObserver = Original;
  });
});
