import { afterAll, beforeAll, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, waitFor } from '@testing-library/react';

import ImageCanvas from './ImageCanvas';

const extractScale = (transform: string) => {
  const match = /scale\(([-\d.]+)\)/.exec(transform);
  return match ? Number.parseFloat(match[1]) : Number.NaN;
};

describe('ImageCanvas', () => {
  const originalRaf = globalThis.requestAnimationFrame;

  beforeAll(() => {
    globalThis.requestAnimationFrame = ((cb: FrameRequestCallback) => {
      cb(0);
      return 0;
    }) as typeof globalThis.requestAnimationFrame;
  });

  afterAll(() => {
    if (originalRaf) {
      globalThis.requestAnimationFrame = originalRaf;
    } else {
      // eslint-disable-next-line @typescript-eslint/no-dynamic-delete
      delete (globalThis as Record<string, unknown>).requestAnimationFrame;
    }
  });

  it('passes props to SmartImage and updates transform on interactions', () => {
    const { container } = render(
      <ImageCanvas thumbSrc="thumb.jpg" src="photo.jpg" alt="Beautiful view" fetchPriority="high" />
    );

    const images = container.querySelectorAll('img');
    const mainImage = images[1] as HTMLImageElement;

    expect(mainImage).toHaveAttribute('alt', 'Beautiful view');
    expect(mainImage).toHaveAttribute('fetchpriority', 'high');

    (mainImage as unknown as { setPointerCapture: (id: number) => void }).setPointerCapture = vi.fn();

    fireEvent.wheel(mainImage, { deltaY: -100 });
    expect(extractScale(mainImage.style.transform)).toBeGreaterThan(1);

    const transformAfterWheel = mainImage.style.transform;

    fireEvent.pointerDown(mainImage, { pointerId: 1, clientX: 10, clientY: 10 });
    fireEvent.pointerMove(mainImage, { pointerId: 1, clientX: 22, clientY: 28, buttons: 1 });
    fireEvent.pointerUp(mainImage, { pointerId: 1 });

    expect(mainImage.style.transform).not.toEqual(transformAfterWheel);

    fireEvent.doubleClick(mainImage);
    expect(extractScale(mainImage.style.transform)).toBeCloseTo(1);
    expect(mainImage.style.transform).toContain('translate(0px, 0px)');

    fireEvent.doubleClick(mainImage);
    expect(extractScale(mainImage.style.transform)).toBeCloseTo(2);
    expect(mainImage.style.transform).toContain('translate(0px, 0px)');
  });

  it('calls onLoaded with image dimensions once the full image loads', async () => {
    const onLoaded = vi.fn();
    const { container } = render(
      <ImageCanvas thumbSrc="thumb.jpg" src="photo.jpg" alt="Beautiful view" fetchPriority="high" onLoaded={onLoaded} />
    );

    const images = container.querySelectorAll('img');
    const mainImage = images[1] as HTMLImageElement;

    Object.defineProperty(mainImage, 'naturalWidth', { configurable: true, value: 1600 });
    Object.defineProperty(mainImage, 'naturalHeight', { configurable: true, value: 900 });

    fireEvent.load(mainImage);

    await waitFor(() => {
      expect(onLoaded).toHaveBeenCalledWith(1600, 900);
    });
  });
});
