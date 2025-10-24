import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import { format, parse } from 'date-fns';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { AccessProfileDto } from '@photobank/shared';

import { EditProfileDialog } from './EditProfileDialog';

const mutateAsyncMock = vi.hoisted(() => vi.fn());

vi.mock('@photobank/shared/api/photobank/admin-access-profiles/admin-access-profiles', () => ({
  getAdminAccessProfilesListQueryKey: () => ['admin-access-profiles'],
  useAdminAccessProfilesUpdate: () => ({
    mutateAsync: mutateAsyncMock,
    isPending: false,
  }),
}));

vi.mock('@photobank/shared/api/photobank/storages/storages', () => ({
  useGetStorages: () => ({
    data: { data: [] },
    isLoading: false,
  }),
}));

vi.mock('@photobank/shared/api/photobank/person-groups/person-groups', () => ({
  usePersonGroupsGetAll: () => ({
    data: { data: [] },
    isLoading: false,
  }),
}));

vi.mock('@/hooks/use-toast', () => ({
  useToast: () => ({
    toast: vi.fn(),
  }),
}));

describe('EditProfileDialog', () => {
  beforeEach(() => {
    mutateAsyncMock.mockReset();
  });

  const renderComponent = (profile: AccessProfileDto) => {
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });

    return render(
      <QueryClientProvider client={queryClient}>
        <EditProfileDialog open onOpenChange={() => {}} profile={profile} />
      </QueryClientProvider>
    );
  };

  it('updates the displayed date when a new value is selected from the calendar', async () => {
    const user = userEvent.setup();

    const profile: AccessProfileDto = {
      id: 1,
      name: 'Editors',
      description: 'Editorial team',
      flags_CanSeeNsfw: false,
      storages: [],
      personGroups: [],
      dateRanges: [
        {
          profileId: 1,
          fromDate: new Date('2024-01-01'),
          toDate: new Date('2024-01-10'),
        },
      ],
      assignedUsersCount: 0,
    };

    renderComponent(profile);

    const fromButton = await screen.findByRole('button', {
      name: /select from date for range 1/i,
    });
    expect(fromButton).toHaveTextContent('2024-01-01');

    await user.click(fromButton);

    const dayButtons = await waitFor(() => {
      const buttons = Array.from(
        document.querySelectorAll<HTMLButtonElement>('button[data-day]')
      );

      if (buttons.length === 0) {
        throw new Error('No calendar day buttons available');
      }

      return buttons;
    });

    const nextTarget =
      dayButtons.find((button) => {
        const label = button.getAttribute('aria-label') ?? '';
        const normalized = label.toLowerCase();
        return (
          label &&
          !normalized.startsWith('today') &&
          !normalized.startsWith('selected')
        );
      }) ?? dayButtons[0];

    if (!nextTarget) {
      throw new Error('Calendar day button not found');
    }

    const dayLabel = nextTarget
      .getAttribute('aria-label')
      ?.replace(/^(Today|Selected),\s*/gi, '');

    if (!dayLabel) {
      throw new Error('Calendar day label not found');
    }

    const parsedDate = parse(dayLabel, 'EEEE, MMMM do, yyyy', new Date());

    if (Number.isNaN(parsedDate.getTime())) {
      throw new Error(`Unable to parse calendar day label: ${dayLabel}`);
    }

    const expectedDisplay = format(parsedDate, 'yyyy-MM-dd');

    await user.click(nextTarget);

    await waitFor(() => {
      expect(fromButton).toHaveTextContent(expectedDisplay);
    });
  });
});
