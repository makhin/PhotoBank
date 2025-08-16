import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const photo = {
  id: 1,
  name: 'Test Photo',
  previewImage: 'fakeImage',
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
  location: { latitude: 10, longitude: 20 },
};
vi.mock('@photobank/shared/api/photobank/photos/photos', () => ({
  usePhotosGetPhoto: () => ({ data: photo, error: undefined }),
  getPhotosGetPhotoQueryKey: vi.fn(),
}));
vi.mock('@photobank/shared/api/photobank', () => ({
  useFacesUpdate: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));

vi.mock('@photobank/shared', async () => {
  const actual = await vi.importActual<any>('@photobank/shared');
  return {
    ...actual,
    getPlaceByGeoPoint: vi.fn().mockResolvedValue('Nice place'),
    useIsAdmin: () => false,
  };
});

import metaReducer from '../src/features/meta/model/metaSlice';

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

  it(
    'renders photo details',
    async () => {
      await renderPage();
      expect(await screen.findByText('Photo Properties')).toBeTruthy();
      expect(await screen.findByDisplayValue('Test Photo')).toBeTruthy();
      expect((await screen.findAllByDisplayValue('1')).length).toBeGreaterThan(0);
      expect(screen.getByLabelText('Show face boxes')).toBeTruthy();
      const placeLink = await screen.findByRole('link', { name: /Nice place/ });
      expect(placeLink).toBeTruthy();
      expect(placeLink.getAttribute('href')).toContain('10,20');
      expect(placeLink.getAttribute('rel')).toBe('noopener noreferrer');
    },
    10000
  );

  it('toggles face boxes visibility', async () => {
    await renderPage();
    const checkbox = screen.getByLabelText('Show face boxes');
    expect(checkbox.getAttribute('data-state')).toBe('unchecked');
    expect(document.querySelectorAll('.face-box').length).toBe(0);
    await userEvent.click(checkbox);
    await waitFor(() => {
      expect(checkbox.getAttribute('data-state')).toBe('checked');
      expect(document.querySelectorAll('.face-box').length).toBeGreaterThan(0);
    });
  });
});
