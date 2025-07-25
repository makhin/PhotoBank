import { NavLink, useLocation } from 'react-router-dom';
import { getAuthToken } from '@photobank/shared/api';
import { useIsAdmin } from '@photobank/shared';
import { Button } from '@/components/ui/button';
import {
  navbarFilterLabel,
  navbarPhotosLabel,
  navbarProfileLabel,
  navbarLoginLabel,
  navbarLogoutLabel,
  navbarRegisterLabel,
  navbarUsersLabel,
} from '@photobank/shared/constants';

export default function NavBar() {
  // useLocation to trigger re-render on route changes
  useLocation();
  const loggedIn = Boolean(getAuthToken());
  const isAdmin = useIsAdmin();

  const linkClass = 'text-sm';

  return (
    <nav className="border-b bg-card p-4">
      <ul className="flex gap-4 items-center">
        <li>
          <Button variant="link" className={linkClass} asChild>
            <NavLink to="/filter">{navbarFilterLabel}</NavLink>
          </Button>
        </li>
        <li>
          <Button variant="link" className={linkClass} asChild>
            <NavLink to="/photos">{navbarPhotosLabel}</NavLink>
          </Button>
        </li>
        <li>
          <Button variant="link" className={linkClass} asChild>
            <NavLink to="/profile">{navbarProfileLabel}</NavLink>
          </Button>
        </li>
        {isAdmin && (
          <li>
            <Button variant="link" className={linkClass} asChild>
              <NavLink to="/admin/users">{navbarUsersLabel}</NavLink>
            </Button>
          </li>
        )}
        {loggedIn ? (
          <li className="ml-auto">
            <Button variant="link" className={linkClass} asChild>
              <NavLink to="/logout">{navbarLogoutLabel}</NavLink>
            </Button>
          </li>
        ) : (
          <>
            <li className="ml-auto">
              <Button variant="link" className={linkClass} asChild>
                <NavLink to="/login">{navbarLoginLabel}</NavLink>
              </Button>
            </li>
            <li>
              <Button variant="link" className={linkClass} asChild>
                <NavLink to="/register">{navbarRegisterLabel}</NavLink>
              </Button>
            </li>
          </>
        )}
      </ul>
    </nav>
  );
}
