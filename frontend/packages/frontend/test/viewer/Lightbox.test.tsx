import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { describe, expect, it } from 'vitest';
import Lightbox from '../../src/features/viewer/Lightbox';
import viewerReducer, { open } from '../../src/features/viewer/viewerSlice';

describe('Lightbox', () => {
  it('navigates and closes', async () => {
    const store = configureStore({ reducer: { viewer: viewerReducer } });
    render(
      <Provider store={store}>
        <Lightbox />
      </Provider>
    );
    const items = [
      { id: 1, preview: 'a_p', title: 'a' },
      { id: 2, preview: 'b_p', title: 'b' },
      { id: 3, preview: 'c_p', title: 'c' },
    ];
    act(() => {
      store.dispatch(open({ items, index: 1 }));
    });
    expect((await screen.findAllByAltText('b')).length).toBeGreaterThan(0);
    await userEvent.keyboard('{ArrowRight}');
    expect((await screen.findAllByAltText('c')).length).toBeGreaterThan(0);
    await userEvent.keyboard('{Escape}');
    await waitFor(() => {
      expect(screen.queryAllByAltText('c').length).toBe(0);
    });
  });
});
