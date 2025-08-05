import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect } from 'vitest';
import { User } from 'lucide-react';
import MetadataBadgeList from './MetadataBadgeList';

describe('MetadataBadgeList', () => {
  it('renders limited items and extra count', () => {
    const map = { 1: 'Alice', 2: 'Bob', 3: 'Carol' };
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
});

