import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { server } from '@/test-setup';
import { scenarioUserLimitedAccess } from '@photobank/shared/api/photobank/msw.scenarios';
import { PhotosPage } from '../PhotosPage';

describe('Photos access policy', () => {
  beforeEach(() => {
    server.resetHandlers(...scenarioUserLimitedAccess());
  });

  it('shows only allowed photos for user', async () => {
    render(<PhotosPage />);
    expect(await screen.findByText(/p_1/i)).toBeInTheDocument();
    expect(screen.queryByText(/p_3/i)).not.toBeInTheDocument();
  });
});
