import React, { forwardRef, useImperativeHandle } from 'react';
import TestRenderer, { act } from 'react-test-renderer';

import type { PhotoItemDto } from '@/api';
import { useInfinitePhotos } from '@/hooks/useInfinitePhotos';

type UseInfinitePhotosResult = ReturnType<typeof useInfinitePhotos>;

type MutateCall = {
  variables: unknown;
  options?: {
    onSuccess?: (data: { items?: PhotoItemDto[] | null; totalCount?: number | null }) => void;
    onError?: (err: Error) => void;
  };
};

const mutateCalls: MutateCall[] = [];

jest.mock('@/api/generated/photos/photos', () => ({
  usePhotosSearchPhotos: jest.fn(() => ({
    mutate: (variables: unknown, options?: MutateCall['options']) => {
      mutateCalls.push({ variables, options });
    },
    isPending: false,
  })),
}));

const TestComponent = forwardRef<UseInfinitePhotosResult, { filter: { caption?: string | null } }>(
  ({ filter }, ref) => {
    const state = useInfinitePhotos({ filter, enabled: true });

    useImperativeHandle(ref, () => state, [state]);

    return null;
  }
);

TestComponent.displayName = 'TestComponent';

describe('useInfinitePhotos', () => {
  beforeEach(() => {
    mutateCalls.length = 0;
  });

  it('ignores stale responses when filters change', async () => {
    const ref = React.createRef<UseInfinitePhotosResult>();

    let renderer: TestRenderer.ReactTestRenderer;

    await act(async () => {
      renderer = TestRenderer.create(<TestComponent ref={ref} filter={{ caption: 'first' }} />);
    });

    expect(mutateCalls).toHaveLength(1);
    const firstCall = mutateCalls[0];

    await act(async () => {
      renderer!.update(<TestComponent ref={ref} filter={{ caption: 'second' }} />);
    });

    expect(mutateCalls).toHaveLength(2);
    const secondCall = mutateCalls[1];

    const oldPhoto: PhotoItemDto = {
      id: 1,
      name: 'old photo',
      storageName: 'storage',
      relativePath: 'old/photo.jpg',
    };

    await act(async () => {
      firstCall.options?.onSuccess?.({
        totalCount: 1,
        items: [oldPhoto],
      });
    });

    expect(ref.current?.photos).toEqual([]);

    const newPhoto: PhotoItemDto = {
      id: 2,
      name: 'new photo',
      storageName: 'storage',
      relativePath: 'new/photo.jpg',
    };

    await act(async () => {
      secondCall.options?.onSuccess?.({
        totalCount: 1,
        items: [newPhoto],
      });
    });

    expect(ref.current?.photos.map((photo) => photo.id)).toEqual([2]);
    expect(ref.current?.totalCount).toBe(1);

    await act(async () => {
      renderer!.unmount();
    });
  });
});
