import { act, render } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useRef } from 'react';

import { useIntersectionObserver } from './useIntersectionObserver';

type ObserverCallback = IntersectionObserverCallback | undefined;

const observeMock = vi.fn();
const disconnectMock = vi.fn();
let observerCallback: ObserverCallback;

class IntersectionObserverMock {
  constructor(callback: IntersectionObserverCallback) {
    observerCallback = callback;
  }

  observe = observeMock;
  disconnect = disconnectMock;
  takeRecords(): IntersectionObserverEntry[] {
    return [];
  }

  unobserve(): void {}
}

describe('useIntersectionObserver', () => {
  beforeEach(() => {
    observerCallback = undefined;
    observeMock.mockClear();
    disconnectMock.mockClear();
    vi.stubGlobal('IntersectionObserver', IntersectionObserverMock as unknown as typeof IntersectionObserver);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  const TestComponent = ({
    enabled = true,
    onIntersect = vi.fn(),
  }: {
    enabled?: boolean;
    onIntersect?: () => void;
  }) => {
    const rootRef = useRef<HTMLDivElement>(null);
    const targetRef = useRef<HTMLDivElement>(null);

    useIntersectionObserver({
      root: rootRef,
      target: targetRef,
      enabled,
      onIntersect,
    });

    return (
      <div ref={rootRef}>
        <div ref={targetRef} />
      </div>
    );
  };

  it('invokes callback when target intersects', () => {
    const onIntersect = vi.fn();

    render(<TestComponent onIntersect={onIntersect} />);

    expect(observeMock).toHaveBeenCalled();

    act(() => {
      observerCallback?.(
        [{ isIntersecting: true } as IntersectionObserverEntry],
        {} as IntersectionObserver
      );
    });

    expect(onIntersect).toHaveBeenCalledTimes(1);
  });

  it('does not create observer when disabled', () => {
    render(<TestComponent enabled={false} />);

    expect(observeMock).not.toHaveBeenCalled();
    expect(observerCallback).toBeUndefined();
  });
});
