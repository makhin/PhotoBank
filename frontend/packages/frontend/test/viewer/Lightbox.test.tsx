import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Lightbox from '@/features/viewer/Lightbox';
import { useViewer } from '@/features/viewer/state';

describe('Lightbox', () => {
  it('navigates and closes', async () => {
    render(<Lightbox />);
    const items = [
      { id: 1, preview: 'a_p', original: 'a', title: 'a' },
      { id: 2, preview: 'b_p', original: 'b', title: 'b' },
      { id: 3, preview: 'c_p', original: 'c', title: 'c' },
    ];
    act(() => {
      useViewer.getState().open(items, 1);
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
