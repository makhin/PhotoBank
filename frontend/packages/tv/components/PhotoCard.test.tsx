import React from 'react';
import renderer, { act } from 'react-test-renderer';
import { describe, it, expect } from 'vitest';

import { PhotoCard } from './PhotoCard';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';

describe('PhotoCard', () => {
  const base: PhotoItemDto = {
    id: 1,
    name: 'Test',
    storageName: 's',
    relativePath: 'r',
  };

  it('renders image from URL when available', () => {
    const photo = { ...base, thumbnailUrl: 'http://example.com/thumb.jpg' };
    let testRenderer: renderer.ReactTestRenderer;
    act(() => {
      testRenderer = renderer.create(<PhotoCard photo={photo} />);
    });
    const image = testRenderer.root.findByProps({ testID: 'photo-image' });
    expect(image.props.source.uri).toBe('http://example.com/thumb.jpg');
  });

  it('renders placeholder when no URL', () => {
    let testRenderer: renderer.ReactTestRenderer;
    act(() => {
      testRenderer = renderer.create(<PhotoCard photo={base} />);
    });
    const placeholder = testRenderer.root.findByProps({ testID: 'photo-placeholder' });
    expect(placeholder).toBeTruthy();
  });
});
