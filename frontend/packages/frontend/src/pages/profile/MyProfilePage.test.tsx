import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi, type Mock } from 'vitest';
import { ProblemDetailsError } from '@photobank/shared/types/problem';

import MyProfilePage from './MyProfilePage';
import { I18nProvider } from '@/app/providers/I18nProvider';
import { useAuthGetUser, useAuthUpdateUser } from '@photobank/shared/api/photobank';
import { toast } from '@/shared/ui/sonner';
import { logger } from '@photobank/shared/utils/logger';

vi.mock('@photobank/shared/api/photobank', () => ({
  useAuthGetUser: vi.fn(),
  useAuthUpdateUser: vi.fn(),
}));

vi.mock('@/shared/ui/sonner', () => ({
  toast: {
    error: vi.fn(),
  },
}));

const useAuthGetUserMock = useAuthGetUser as unknown as Mock;
const useAuthUpdateUserMock = useAuthUpdateUser as unknown as Mock;

const renderPage = () =>
  render(
    <I18nProvider>
      <MemoryRouter>
        <MyProfilePage />
      </MemoryRouter>
    </I18nProvider>
  );

describe('MyProfilePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows toast with problem details when profile update fails', async () => {
    const problem = new ProblemDetailsError({
      title: 'Profile update failed',
      status: 400,
      detail: 'Phone number is invalid',
    });

    useAuthGetUserMock.mockReturnValue({
      data: {
        data: {
          email: 'user@example.com',
          phoneNumber: '',
          telegramUserId: null,
        },
      },
    });

    const mutateAsync = vi.fn().mockRejectedValue(problem);
    useAuthUpdateUserMock.mockReturnValue({ mutateAsync });

    const loggerErrorSpy = vi.spyOn(logger, 'error').mockImplementation(() => {});

    renderPage();

    const user = userEvent.setup();
    const saveButton = await screen.findByRole('button', { name: /save/i });
    await user.click(saveButton);

    await waitFor(() =>
      expect(toast.error).toHaveBeenCalledWith(problem.problem.title, {
        description: problem.problem.detail,
      })
    );

    expect(loggerErrorSpy).toHaveBeenCalledWith(problem.problem);
    loggerErrorSpy.mockRestore();
  });

  it('submits telegram ID as string without coercion', async () => {
    useAuthGetUserMock.mockReturnValue({
      data: {
        data: {
          email: 'user@example.com',
          phoneNumber: '',
          telegramUserId: null,
        },
      },
    });

    const mutateAsync = vi.fn().mockResolvedValue(undefined);
    useAuthUpdateUserMock.mockReturnValue({ mutateAsync });

    renderPage();

    const user = userEvent.setup();
    const telegramInput = await screen.findByLabelText(/telegram/i);
    await user.clear(telegramInput);
    await user.type(telegramInput, '9007199254740995');

    const saveButton = await screen.findByRole('button', { name: /save/i });
    await user.click(saveButton);

    await waitFor(() =>
      expect(mutateAsync).toHaveBeenCalledWith({
        data: {
          phoneNumber: '',
          telegramUserId: '9007199254740995',
        },
      })
    );
  });
});
