import { NavLink, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { getAuthToken } from '@photobank/shared/auth';
import { useIsAdmin } from '@photobank/shared';

import LanguageSwitcher from '@/components/LanguageSwitcher';
import { Button } from '@/shared/ui/button';

export default function NavBar() {
  // useLocation to trigger re-render on route changes
  useLocation();
  const loggedIn = Boolean(getAuthToken());
  const isAdmin = useIsAdmin();
  const { t } = useTranslation();

  const linkClass = 'text-sm';
  const adminLinks = [
    { to: '/admin/users', label: t('navbarUsersLabel') },
    { to: '/admin/access-profiles', label: t('navbarAccessProfilesLabel') },
    { to: '/admin/person-groups', label: t('navbarPersonGroupsLabel') },
    { to: '/admin/persons', label: t('navbarPersonsLabel') },
    { to: '/admin/faces', label: t('navbarFacesLabel') },
  ];

  return (
    <nav className="border-b bg-card p-4">
      <ul className="flex gap-4 items-center">
        <li>
          <Button variant="link" className={linkClass} asChild>
            <NavLink to="/filter">{t('navbarFilterLabel')}</NavLink>
          </Button>
        </li>
        <li>
          <Button variant="link" className={linkClass} asChild>
            <NavLink to="/photos">{t('navbarPhotosLabel')}</NavLink>
          </Button>
        </li>
        <li>
          <Button variant="link" className={linkClass} asChild>
            <NavLink to="/profile">{t('navbarProfileLabel')}</NavLink>
          </Button>
        </li>
        <li>
          <Button variant="link" className={linkClass} asChild>
            <NavLink to="/openai">{t('navbarOpenAiLabel')}</NavLink>
          </Button>
        </li>
        {isAdmin &&
          adminLinks.map(({ to, label }) => (
            <li key={to}>
              <Button variant="link" className={linkClass} asChild>
                <NavLink to={to}>{label}</NavLink>
              </Button>
            </li>
          ))}
        {loggedIn ? (
          <li className="ml-auto">
            <Button variant="link" className={linkClass} asChild>
              <NavLink to="/logout">{t('navbarLogoutLabel')}</NavLink>
            </Button>
          </li>
        ) : (
          <>
            <li className="ml-auto">
              <Button variant="link" className={linkClass} asChild>
                <NavLink to="/login">{t('navbarLoginLabel')}</NavLink>
              </Button>
            </li>
            <li>
              <Button variant="link" className={linkClass} asChild>
                <NavLink to="/register">{t('navbarRegisterLabel')}</NavLink>
              </Button>
            </li>
          </>
        )}
        <li>
          <LanguageSwitcher />
        </li>
      </ul>
    </nav>
  );
}
