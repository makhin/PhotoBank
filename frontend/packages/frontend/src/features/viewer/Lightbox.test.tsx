import type { ComponentProps, ReactElement } from 'react';

import { fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { close, next, prev } from './viewerSlice';
import Lightbox from './Lightbox';
import type ImageCanvas from './ImageCanvas';
import { useAppDispatch, useAppSelector } from '@/app/hook';

type ImageCanvasProps = ComponentProps<typeof ImageCanvas>;

const { imageCanvasMock } = vi.hoisted(() => ({
  imageCanvasMock: vi.fn<(props: ImageCanvasProps) => ReactElement>(() => (
    <div data-testid="image-canvas" />
  )),
}));

vi.mock('@/app/hook', () => ({
  useAppDispatch: vi.fn(),
  useAppSelector: vi.fn(),
}));

vi.mock('./ImageCanvas', () => ({
  __esModule: true,
  default: imageCanvasMock,
}));

describe('Lightbox', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.mocked(useAppSelector).mockReset();
    vi.mocked(useAppDispatch).mockReset();
  });

  it('renders nothing when the viewer is closed', () => {
    vi.mocked(useAppDispatch).mockReturnValue(vi.fn());
    vi.mocked(useAppSelector).mockImplementation((selector) =>
      selector({ viewer: { isOpen: false, items: [], index: 0 } } as never)
    );

    const { container } = render(<Lightbox />);

    expect(container).toBeEmptyDOMElement();
    expect(imageCanvasMock).not.toHaveBeenCalled();
  });

  it('renders the active item and dispatches viewer actions', () => {
    const dispatch = vi.fn();
    const state = {
      viewer: {
        isOpen: true,
        index: 0,
        items: [
          { id: 1, preview: 'first.jpg', title: 'First image' },
          { id: 2, preview: 'second.jpg', title: 'Second image' },
        ],
      },
    } as const;

    vi.mocked(useAppDispatch).mockReturnValue(dispatch);
    vi.mocked(useAppSelector).mockImplementation((selector) => selector(state as never));

    render(<Lightbox />);

    expect(screen.getByText('1 / 2')).toBeInTheDocument();

    const firstCall = imageCanvasMock.mock.calls[0];
    if (!firstCall || !firstCall[0]) {
      throw new Error('ImageCanvas was not called with props');
    }
    const canvasProps = firstCall[0];
    expect(canvasProps).toMatchObject({
      thumbSrc: 'first.jpg',
      src: 'first.jpg',
      alt: 'First image',
      fetchPriority: 'high',
    });

    fireEvent.click(screen.getByRole('button', { name: /Close/i }));
    fireEvent.click(screen.getByRole('button', { name: /Next image/i }));
    fireEvent.click(screen.getByRole('button', { name: /Previous image/i }));

    fireEvent.keyDown(window, { key: 'Escape' });
    fireEvent.keyDown(window, { key: 'ArrowRight' });
    fireEvent.keyDown(window, { key: 'ArrowLeft' });

    expect(dispatch).toHaveBeenCalledWith(close());
    expect(dispatch).toHaveBeenCalledWith(next());
    expect(dispatch).toHaveBeenCalledWith(prev());
  });
});
