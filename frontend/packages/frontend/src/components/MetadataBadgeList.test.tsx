import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { User } from 'lucide-react';

import MetadataBadgeList from './MetadataBadgeList';

describe('MetadataBadgeList', () => {
  it('renders limited items and extra count', () => {
    const map = new Map<number, string>([
      [1, 'Alice'],
      [2, 'Bob'],
      [3, 'Carol'],
    ]);
    render(
      <MetadataBadgeList
        icon={User}
        items={[1, 2, 3]}
        map={map}
        maxVisible={2}
        variant="outline"
      />
    );
    expect(screen.getByText('Alice')).toBeInTheDocument();
    expect(screen.getByText('Bob')).toBeInTheDocument();
    expect(screen.getByText('+1')).toBeInTheDocument();
  });

  it('renders string labels without an icon', () => {
    const { container } = render(
      <MetadataBadgeList
        items={['Alice', 'Bob', 'Carol']}
        maxVisible={2}
        variant="outline"
      />,
    );

    expect(screen.getByText('Alice')).toBeInTheDocument();
    expect(screen.getByText('Bob')).toBeInTheDocument();
    expect(screen.getByText('+1')).toBeInTheDocument();
    expect(container.querySelector('svg')).toBeNull();
  });
});

