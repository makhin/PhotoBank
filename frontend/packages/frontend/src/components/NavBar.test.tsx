import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, beforeEach, vi, expect } from 'vitest';

import NavBar from './NavBar';
import { getAuthToken } from '@photobank/shared/auth';
import { useIsAdmin } from '@photobank/shared';

const changeLanguageMock = vi.fn();
const i18nMock = {
  language: 'en',
  changeLanguage: changeLanguageMock,
};

vi.mock('@photobank/shared/auth', () => ({
  getAuthToken: vi.fn(),
}));

vi.mock('@photobank/shared', () => ({
  useIsAdmin: vi.fn(),
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: i18nMock,
  }),
}));

const renderNavBar = () =>
  render(
    <MemoryRouter initialEntries={['/']}>
      <NavBar />
    </MemoryRouter>
  );

describe('NavBar', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    i18nMock.language = 'en';
    changeLanguageMock.mockClear();
  });

  it('does not render admin dropdown trigger for non-admin users', () => {
    vi.mocked(getAuthToken).mockReturnValue('token');
    vi.mocked(useIsAdmin).mockReturnValue(false);

    renderNavBar();

    expect(
      screen.queryByRole('button', { name: 'navbarAdminLabel' })
    ).not.toBeInTheDocument();
  });

  it('renders admin dropdown with admin links for admin users', async () => {
    const user = userEvent.setup();
    vi.mocked(getAuthToken).mockReturnValue('token');
    vi.mocked(useIsAdmin).mockReturnValue(true);

    renderNavBar();

    const adminTrigger = screen.getByRole('button', {
      name: 'navbarAdminLabel',
    });

    await user.click(adminTrigger);

    const adminLinkLabels = [
      'navbarUsersLabel',
      'navbarAccessProfilesLabel',
      'navbarPersonGroupsLabel',
      'navbarPersonsLabel',
      'navbarFacesLabel',
    ];

    for (const label of adminLinkLabels) {
      expect(await screen.findByText(label)).toBeInTheDocument();
    }
  });

  it('opens the mobile menu when the menu button is clicked', async () => {
    const user = userEvent.setup();
    vi.mocked(getAuthToken).mockReturnValue('token');
    vi.mocked(useIsAdmin).mockReturnValue(false);

    renderNavBar();

    const menuButton = screen.getByRole('button', {
      name: 'navbarMenuLabel',
    });

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();

    await user.click(menuButton);

    const dialog = await screen.findByRole('dialog');
    expect(
      within(dialog).getByRole('link', { name: 'navbarPhotosLabel' })
    ).toBeInTheDocument();
  });
});
