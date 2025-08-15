import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Lightbox from '@/features/viewer/Lightbox';
import { useViewer } from '@/features/viewer/state';

describe('Lightbox', () => {
  it('navigates and closes', async () => {
    render(<Lightbox />);
    const items = [
      { id: 1, src: 'a', title: 'a' },
      { id: 2, src: 'b', title: 'b' },
      { id: 3, src: 'c', title: 'c' },
    ];
    useViewer.getState().open(items, 1);
    expect(screen.getByAltText('b')).toBeInTheDocument();
    await userEvent.keyboard('{ArrowRight}');
    expect(screen.getByAltText('c')).toBeInTheDocument();
    await userEvent.keyboard('{Escape}');
    expect(screen.queryByAltText('c')).not.toBeInTheDocument();
  });
});
