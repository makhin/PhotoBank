import React from 'react';
import { render, fireEvent } from '@testing-library/react';
import SmartImage from '@/components/SmartImage';
import { vi } from 'vitest';

describe('SmartImage', () => {
  beforeEach(() => {
    vi.stubGlobal('requestAnimationFrame', (cb: FrameRequestCallback) => {
      cb(0);
      return 0;
    });
    vi.stubGlobal(
      'Image',
      class {
        onload: (() => void) | null = null;
        onerror: (() => void) | null = null;
        set src(_v: string) {
          this.onload && this.onload();
        }
      } as any
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('fades in full image after load', () => {
    const { container } = render(
      <SmartImage alt="alt" thumbSrc="thumb" src="full" />
    );
    const imgs = container.querySelectorAll('img');
    const thumb = imgs[0];
    const full = imgs[1];
    expect(thumb.className).toMatch(/opacity-100/);
    expect(full.className).toMatch(/opacity-0/);
    fireEvent.load(full);
    expect(full.className).toMatch(/opacity-100/);
    expect(thumb.className).toMatch(/opacity-0/);
  });
});
