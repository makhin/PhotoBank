import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi, type Mock, beforeAll, afterAll } from 'vitest';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';
import { DEFAULT_PHOTO_FILTER, METADATA_CACHE_VERSION } from '@photobank/shared/constants';

import metaReducer from '@/features/meta/model/metaSlice';
import photoReducer from '@/features/photo/model/photoSlice';
import viewerReducer from '@/features/viewer/viewerSlice';
import PhotoListPage from './PhotoListPage';
import { usePhotoListAdapter } from '@/features/photo/usePhotoListAdapter';
import { useVirtualizer } from '@tanstack/react-virtual';
import { I18nProvider } from '@/app/providers/I18nProvider';

vi.mock('@/features/photo/usePhotoListAdapter');
vi.mock('@tanstack/react-virtual', () => ({
  useVirtualizer: vi.fn(),
}));

class IntersectionObserverMock {
  constructor(private readonly callback: IntersectionObserverCallback) {}
  observe() {
    const entry = [{ isIntersecting: true } as IntersectionObserverEntry];
    this.callback(entry, this as unknown as IntersectionObserver);
  }
  unobserve() {}
  disconnect() {}
  takeRecords(): IntersectionObserverEntry[] {
    return [];
  }
}

beforeAll(() => {
  vi.stubGlobal('IntersectionObserver', IntersectionObserverMock);
});

afterAll(() => {
  vi.unstubAllGlobals();
});

const createPhotos = (count: number): PhotoItemDto[] =>
  Array.from({ length: count }, (_, i) => ({
    id: i + 1,
    thumbnailUrl: '',
    name: `Photo ${i + 1}`,
    storageName: 's',
    relativePath: 'p',
  }));

test('renders PhotoTable and fetches next page when scrolled', async () => {
  const photos = createPhotos(10);
  const fetchNextPage = vi.fn();

  (usePhotoListAdapter as unknown as Mock).mockReturnValue({
    photos,
    counters: {
      total: 20,
      loaded: photos.length,
      flags: { bw: 0, adult: 0, racy: 0 },
    },
    total: 20,
    fetchNextPage,
    hasNextPage: true,
    isLoading: false,
    isFetchingNextPage: false,
  });

  const initialVirtual = photos.map((_, i) => ({
    key: i,
    index: i,
    start: i * 92,
    size: 92,
    end: (i + 1) * 92,
    lane: 0,
  }));
  const withLoader = [
    ...initialVirtual,
    {
      key: photos.length,
      index: photos.length,
      start: photos.length * 92,
      size: 92,
      end: (photos.length + 1) * 92,
      lane: 0,
    },
  ];
  (useVirtualizer as unknown as Mock).mockReturnValue({
    getVirtualItems: vi
      .fn()
      .mockReturnValueOnce(initialVirtual)
      .mockReturnValueOnce(withLoader)
      .mockReturnValue(withLoader),
    getTotalSize: () => 1000,
    measureElement: vi.fn(),
  });

  const store = configureStore({
    reducer: {
      metadata: metaReducer,
      photo: photoReducer,
      viewer: viewerReducer,
    },
    preloadedState: {
      metadata: {
        tags: [],
        persons: [],
        paths: [],
        storages: [],
        version: METADATA_CACHE_VERSION,
        loaded: true,
        loading: false,
        error: undefined,
      },
      photo: { filter: { ...DEFAULT_PHOTO_FILTER }, selectedPhotos: [] },
      viewer: { isOpen: false, items: [], index: 0 },
    },
  });

  const queryClient = new QueryClient();

  const wrapper = ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <Provider store={store}>
        <I18nProvider>
          <MemoryRouter>{children}</MemoryRouter>
        </I18nProvider>
      </Provider>
    </QueryClientProvider>
  );

  const { rerender } = render(<PhotoListPage />, { wrapper });

  expect(await screen.findByText('Preview')).toBeInTheDocument();

  rerender(<PhotoListPage />);

  expect(fetchNextPage).toHaveBeenCalled();
});
