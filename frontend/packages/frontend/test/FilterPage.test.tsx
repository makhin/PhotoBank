import React from 'react';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import metaReducer from '../src/features/meta/model/metaSlice';
import photoReducer from '../src/features/photo/model/photoSlice';
import { DEFAULT_PHOTO_FILTER } from '@photobank/shared/constants';
import { METADATA_CACHE_VERSION } from '@photobank/shared/constants';

vi.mock('@photobank/shared', async () => {
  const actual = await vi.importActual<any>('@photobank/shared');
  return { ...actual, useIsAdmin: () => false };
});

const initialMeta = {
  tags: [],
  persons: [],
  paths: [],
  storages: [],
  version: METADATA_CACHE_VERSION,
  loaded: false,
  loading: false,
  error: undefined,
};

const renderPage = async (preloaded: any) => {
  const store = configureStore({
    reducer: { metadata: metaReducer, photo: photoReducer },
    preloadedState: {
      metadata: { ...initialMeta, ...preloaded },
      photo: { filter: { ...DEFAULT_PHOTO_FILTER }, selectedPhotos: [], lastResult: [] },
    },
  });

  const queryClient = new QueryClient();

  const { default: FilterPage } = await import('../src/pages/filter/FilterPage');

  render(
    <QueryClientProvider client={queryClient}>
      <Provider store={store}>
        <MemoryRouter initialEntries={["/filter"]}>
          <Routes>
            <Route path="/filter" element={<FilterPage />} />
          </Routes>
        </MemoryRouter>
      </Provider>
    </QueryClientProvider>
  );

  return store;
};

describe('FilterPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading text when metadata not loaded', async () => {
    await renderPage({ loaded: false, loading: true });
    expect(screen.getByText('Loading...')).toBeTruthy();
  });

  it('renders filter form when metadata loaded', async () => {
    await renderPage({ loaded: true });
    expect(await screen.findByText('Caption')).toBeTruthy();
  });
});
