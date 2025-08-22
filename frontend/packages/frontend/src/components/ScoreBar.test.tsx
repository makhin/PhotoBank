import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';

import { ScoreBar } from './ScoreBar';

describe('ScoreBar', () => {
  it('displays label and percentage with correct width', () => {
    const { container } = render(
      <ScoreBar label="Accuracy" score={0.5} colorClass="bg-green-500" />
    );

    expect(screen.getByText('Accuracy')).toBeInTheDocument();
    expect(screen.getByText('50.0%')).toBeInTheDocument();
    const bar = container.querySelector('.flex-1 > div');
    expect(bar).toHaveStyle({ width: '50.0%' });
  });
});

