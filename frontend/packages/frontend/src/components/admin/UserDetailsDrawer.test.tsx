import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { AccessProfileDto, UserDto } from '@photobank/shared';

import { UserDetailsDrawer } from './UserDetailsDrawer';

const listMock = vi.fn();
const invalidateQueriesMock = vi.fn();
const toastMock = vi.fn();
const updateMutationMock = {
  mutateAsync: vi.fn(),
  isPending: false,
};

if (!Element.prototype.hasPointerCapture) {
  Element.prototype.hasPointerCapture = () => {};
}

if (!Element.prototype.releasePointerCapture) {
  Element.prototype.releasePointerCapture = () => {};
}

if (!Element.prototype.scrollIntoView) {
  Element.prototype.scrollIntoView = () => {};
}

vi.mock(
  '@tanstack/react-query',
  () => ({
    useQueryClient: () => ({
      invalidateQueries: invalidateQueriesMock,
    }),
    useQuery: vi.fn(),
    useMutation: vi.fn(),
  }),
  { virtual: true }
);

vi.mock('@photobank/shared/api/photobank', () => ({
  useUsersUpdate: () => updateMutationMock,
  getUsersGetAllQueryKey: () => ['/admin/users'],
}));

vi.mock('@photobank/shared/api/photobank/admin-access-profiles/admin-access-profiles', () => ({
  getAdminAccessProfilesListQueryKey: () => ['admin-access-profiles'],
  useAdminAccessProfilesList: () => listMock(),
  useAdminAccessProfilesAssignUser: () => ({ mutateAsync: vi.fn(), isPending: false }),
  useAdminAccessProfilesUnassignUser: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));

vi.mock('@/hooks/use-toast', () => ({
  useToast: () => ({
    toast: toastMock,
  }),
}));

describe('UserDetailsDrawer', () => {
  beforeEach(() => {
    listMock.mockReset();
    invalidateQueriesMock.mockReset();
    toastMock.mockReset();
    updateMutationMock.mutateAsync.mockReset();
    updateMutationMock.isPending = false;
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

    render(<UserDetailsDrawer user={user} open onOpenChange={() => {}} onUserUpdated={() => {}} />);

    const userActions = userEvent.setup();
    await userActions.click(screen.getByRole('tab', { name: /access/i }));

    expect(await screen.findByText('Assigned')).toBeInTheDocument();
  });

  it('updates telegram settings when form is submitted', async () => {
    const user: UserDto = {
      id: 'user-456',
      email: 'telegram@example.com',
      phoneNumber: null,
      roles: ['Admin'],
      telegramUserId: null,
      telegramSendTimeUtc: null,
      accessProfiles: [],
    };

    listMock.mockReturnValue({
      data: { data: [] },
      isLoading: false,
      isError: false,
      isFetching: false,
      refetch: vi.fn(),
    });

    updateMutationMock.mutateAsync.mockResolvedValue({ status: 200, data: undefined });

    const onUserUpdated = vi.fn();
    render(
      <UserDetailsDrawer
        user={user}
        open
        onOpenChange={() => {}}
        onUserUpdated={onUserUpdated}
      />
    );

    const ui = userEvent.setup();

    const telegramInput = screen.getByLabelText(/telegram id/i);
    await ui.type(telegramInput, '987654');

    await ui.click(screen.getByLabelText(/send time/i));
    const sendTimeOption = await screen.findByRole('option', { name: '09:00:00' });
    await ui.click(sendTimeOption);

    await ui.click(screen.getByRole('button', { name: /save telegram settings/i }));

    await waitFor(() => {
      expect(updateMutationMock.mutateAsync).toHaveBeenCalledWith({
        id: 'user-456',
        data: {
          telegramUserId: '987654',
          telegramSendTimeUtc: '09:00:00',
        },
      });
    });

    expect(onUserUpdated).toHaveBeenCalledWith(
      expect.objectContaining({
        telegramUserId: '987654',
        telegramSendTimeUtc: '09:00:00',
      })
    );
    expect(invalidateQueriesMock).toHaveBeenCalledWith({ queryKey: ['/admin/users'] });
  });

  it('resets telegram form when switching to a different user with identical values', async () => {
    const baseUser: UserDto = {
      id: 'user-aaa',
      email: 'first@example.com',
      phoneNumber: null,
      roles: ['Viewer'],
      telegramUserId: null,
      telegramSendTimeUtc: null,
      accessProfiles: [],
    };

    listMock.mockReturnValue({
      data: { data: [] },
      isLoading: false,
      isError: false,
      isFetching: false,
      refetch: vi.fn(),
    });

    const { rerender } = render(
      <UserDetailsDrawer
        user={baseUser}
        open
        onOpenChange={() => {}}
        onUserUpdated={() => {}}
      />
    );

    const ui = userEvent.setup();
    const telegramInput = screen.getByLabelText(/telegram id/i);

    await ui.type(telegramInput, '13579');
    expect(telegramInput).toHaveValue('13579');

    const nextUser: UserDto = {
      ...baseUser,
      id: 'user-bbb',
      email: 'second@example.com',
    };

    rerender(
      <UserDetailsDrawer
        user={nextUser}
        open
        onOpenChange={() => {}}
        onUserUpdated={() => {}}
      />
    );

    await waitFor(() => {
      expect(screen.getByLabelText(/telegram id/i)).toHaveValue('');
    });
  });
});
