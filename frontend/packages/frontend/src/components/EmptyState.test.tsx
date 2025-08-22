import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';

import EmptyState from './EmptyState';

describe('EmptyState', () => {
  it('renders icon and text', () => {
    const { container } = render(<EmptyState text="Nothing here" />);
    expect(screen.getByText('Nothing here')).toBeInTheDocument();
    expect(container.querySelector('svg[aria-hidden="true"]')).toBeInTheDocument();
  });
});

