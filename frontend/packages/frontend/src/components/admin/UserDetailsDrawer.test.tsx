import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { AccessProfileDto, UserDto } from '@photobank/shared';

import { UserDetailsDrawer } from './UserDetailsDrawer';

const listMock = vi.fn();

vi.mock('@tanstack/react-query', () => ({
  useQueryClient: () => ({
    invalidateQueries: vi.fn(),
  }),
}));

vi.mock('@photobank/shared/api/photobank/admin-access-profiles/admin-access-profiles', () => ({
  getAdminAccessProfilesListQueryKey: () => ['admin-access-profiles'],
  useAdminAccessProfilesList: () => listMock(),
  useAdminAccessProfilesAssignUser: () => ({ mutateAsync: vi.fn(), isPending: false }),
  useAdminAccessProfilesUnassignUser: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));

vi.mock('@/hooks/use-toast', () => ({
  useToast: () => ({
    toast: vi.fn(),
  }),
}));

describe('UserDetailsDrawer', () => {
  beforeEach(() => {
    listMock.mockReset();
  });

  it('renders assigned badge when user already has the profile', async () => {
    const accessProfiles: AccessProfileDto[] = [
      {
        id: 7,
        name: 'Night Shift',
        description: 'Access to night shift photos',
        flags_CanSeeNsfw: false,
        storages: [],
        personGroups: [],
        dateRanges: [],
        assignedUsersCount: 1,
      },
    ];

    const user: UserDto = {
      id: 'user-123',
      email: 'user@example.com',
      phoneNumber: '555-0100',
      roles: ['Viewer'],
      telegramUserId: null,
      telegramSendTimeUtc: null,
      accessProfiles: [{ profileId: 7 }],
    };

    listMock.mockReturnValue({
      data: { data: accessProfiles },
      isLoading: false,
      isError: false,
      isFetching: false,
      refetch: vi.fn(),
    });

    render(<UserDetailsDrawer user={user} open onOpenChange={() => {}} />);

    const userActions = userEvent.setup();
    await userActions.click(screen.getByRole('tab', { name: /access/i }));

    expect(await screen.findByText('Assigned')).toBeInTheDocument();
  });
});
