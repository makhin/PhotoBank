import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { AccessProfileDto } from '@photobank/shared';

import { AccessProfilesGrid } from './AccessProfilesGrid';

vi.mock('@photobank/shared/api/photobank/admin-access-profiles/admin-access-profiles', () => ({
  useAdminAccessProfilesDelete: () => ({ mutate: vi.fn(), isPending: false }),
}));

vi.mock('@/hooks/use-toast', () => ({
  useToast: () => ({
    toast: vi.fn(),
  }),
}));

describe('AccessProfilesGrid', () => {
  it('renders assigned user count for each profile', () => {
    const profiles: AccessProfileDto[] = [
      {
        id: 1,
        name: 'Moderators',
        description: 'Moderation team',
        flags_CanSeeNsfw: false,
        storages: [],
        personGroups: [],
        dateRanges: [],
        assignedUsersCount: 3,
      },
    ];

    render(<AccessProfilesGrid profiles={profiles} onEditProfile={vi.fn()} />);

    expect(screen.getByText('3 users')).toBeInTheDocument();
  });
});
