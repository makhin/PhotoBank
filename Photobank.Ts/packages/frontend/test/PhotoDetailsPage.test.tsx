import React from 'react';
import { render, screen } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import metaReducer from '../src/features/meta/model/metaSlice';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const photo = {
  id: 1,
  name: 'Test Photo',
  previewImage: '',
  scale: 1,
  takenDate: '2024-01-01T00:00:00Z',
  faces: [
    {
      id: 1,
      personId: 1,
      age: 30,
      gender: true,
      friendlyFaceAttributes: 'Happy',
      faceBox: { top: 0, left: 0, width: 10, height: 10 },
    },
  ],
  captions: ['Caption'],
  tags: ['tag1'],
  adultScore: 0,
  racyScore: 0,
  height: 100,
  width: 200,
  orientation: 1,
};

vi.mock('../src/entities/photo/api.ts', () => ({
  useGetPhotoByIdQuery: () => ({ data: photo, error: undefined }),
}));

class RO {
  observe() {}
  unobserve() {}
  disconnect() {}
}
// @ts-ignore
global.ResizeObserver = RO;

const renderPage = async () => {
  const store = configureStore({
    reducer: { metadata: metaReducer },
    preloadedState: {
      metadata: {
        tags: [],
        persons: [{ id: 1, name: 'John' }],
        paths: [],
        storages: [],
        version: 1,
        loaded: true,
        loading: false,
        error: undefined,
      },
    },
  });

  const { default: PhotoDetailsPage } = await import('../src/pages/detail/PhotoDetailsPage');

  render(
    <Provider store={store}>
      <MemoryRouter initialEntries={["/photos/1"]}>
        <Routes>
          <Route path="/photos/:id" element={<PhotoDetailsPage />} />
        </Routes>
      </MemoryRouter>
    </Provider>
  );
};

describe('PhotoDetailsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders photo details', async () => {
    await renderPage();
    expect(screen.getByText('Photo Properties')).toBeTruthy();
    expect(screen.getByDisplayValue('Test Photo')).toBeTruthy();
    expect(screen.getAllByDisplayValue('1').length).toBeGreaterThan(0);
  });
});
