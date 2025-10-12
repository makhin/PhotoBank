import { useState, type ReactNode } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { getAuthToken } from '@photobank/shared/auth';
import { useIsAdmin } from '@photobank/shared';
import { MenuIcon } from 'lucide-react';

import LanguageSwitcher from '@/components/LanguageSwitcher';
import { Button } from '@/shared/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/shared/ui/dropdown-menu';
import {
  Sheet,
  SheetClose,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from '@/shared/ui/sheet';
import { cn } from '@/shared/lib/utils';

type BaseLink = {
  key: string;
  to: string;
  label: string;
  align?: 'end';
};

type NavItem =
  | ({ type: 'link' } & BaseLink)
  | {
      type: 'admin';
      key: string;
      label: string;
      links: BaseLink[];
    }
  | {
      type: 'component';
      key: string;
      element: ReactNode;
    };

type NavItemsListProps = {
  items: NavItem[];
  variant: 'desktop' | 'mobile';
  onNavigate?: () => void;
};

const linkClass = 'text-sm';

function NavItemsList({ items, variant, onNavigate }: NavItemsListProps) {
  const Container = variant === 'desktop' ? 'ul' : 'div';
  const ItemWrapper = variant === 'desktop' ? 'li' : 'div';
  const containerClass =
    variant === 'desktop'
      ? 'hidden sm:flex items-center gap-4'
      : 'flex flex-col gap-2';

  const renderLinkButton = (link: BaseLink) => (
    <Button
      variant="link"
      className={cn(
        linkClass,
        variant === 'mobile' && 'justify-start px-0 w-full text-base'
      )}
      asChild
    >
      <NavLink to={link.to} onClick={onNavigate}>
        {link.label}
      </NavLink>
    </Button>
  );

  return (
    <Container className={containerClass}>
      {items.flatMap((item) => {
        if (item.type === 'link') {
          const content =
            variant === 'mobile' ? (
              <SheetClose asChild>{renderLinkButton(item)}</SheetClose>
            ) : (
              renderLinkButton(item)
            );

          return (
            <ItemWrapper
              key={item.key}
              className={cn(
                variant === 'desktop' && item.align === 'end' && 'ml-auto'
              )}
            >
              {content}
            </ItemWrapper>
          );
        }

        if (item.type === 'admin') {
          if (variant === 'desktop') {
            return (
              <ItemWrapper key={item.key}>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="link" className={linkClass}>
                      {item.label}
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="start">
                    {item.links.map((link) => (
                      <DropdownMenuItem key={link.key} asChild>
                        <NavLink to={link.to} className={linkClass}>
                          {link.label}
                        </NavLink>
                      </DropdownMenuItem>
                    ))}
                  </DropdownMenuContent>
                </DropdownMenu>
              </ItemWrapper>
            );
          }

          return item.links.map((link) => (
            <ItemWrapper key={`${item.key}-${link.key}`}>
              <SheetClose asChild>
                {renderLinkButton(link)}
              </SheetClose>
            </ItemWrapper>
          ));
        }

        return (
          <ItemWrapper key={item.key}>
            {variant === 'mobile' ? (
              <div className="pt-2">
                {item.element}
              </div>
            ) : (
              item.element
            )}
          </ItemWrapper>
        );
      })}
    </Container>
  );
}

export default function NavBar() {
  // useLocation to trigger re-render on route changes
  useLocation();
  const loggedIn = Boolean(getAuthToken());
  const isAdmin = useIsAdmin();
  const { t } = useTranslation();

  const [isMenuOpen, setIsMenuOpen] = useState(false);

  const adminLinks: BaseLink[] = [
    { key: 'admin-users', to: '/admin/users', label: t('navbarUsersLabel') },
    {
      key: 'admin-access-profiles',
      to: '/admin/access-profiles',
      label: t('navbarAccessProfilesLabel'),
    },
    {
      key: 'admin-person-groups',
      to: '/admin/person-groups',
      label: t('navbarPersonGroupsLabel'),
    },
    { key: 'admin-persons', to: '/admin/persons', label: t('navbarPersonsLabel') },
    { key: 'admin-faces', to: '/admin/faces', label: t('navbarFacesLabel') },
  ];

  const baseLinks: BaseLink[] = [
    { key: 'filter', to: '/filter', label: t('navbarFilterLabel') },
    { key: 'photos', to: '/photos', label: t('navbarPhotosLabel') },
    { key: 'profile', to: '/profile', label: t('navbarProfileLabel') },
    { key: 'openai', to: '/openai', label: t('navbarOpenAiLabel') },
  ];

  const navItems: NavItem[] = [
    ...baseLinks.map((link) => ({ ...link, type: 'link' as const })),
  ];

  if (isAdmin) {
    navItems.push({
      type: 'admin',
      key: 'admin',
      label: t('navbarAdminLabel'),
      links: adminLinks,
    });
  }

  if (loggedIn) {
    navItems.push({
      type: 'link',
      key: 'logout',
      to: '/logout',
      label: t('navbarLogoutLabel'),
      align: 'end',
    });
  } else {
    navItems.push({
      type: 'link',
      key: 'login',
      to: '/login',
      label: t('navbarLoginLabel'),
      align: 'end',
    });
    navItems.push({
      type: 'link',
      key: 'register',
      to: '/register',
      label: t('navbarRegisterLabel'),
    });
  }

  navItems.push({
    type: 'component',
    key: 'language',
    element: <LanguageSwitcher />,
  });

  return (
    <nav className="border-b bg-card p-4">
      <div className="flex items-center justify-between sm:hidden">
        <Sheet open={isMenuOpen} onOpenChange={setIsMenuOpen}>
          <SheetTrigger asChild>
            <Button
              variant="outline"
              size="icon"
              aria-label={t('navbarMenuLabel')}
            >
              <MenuIcon className="h-5 w-5" />
            </Button>
          </SheetTrigger>
          <SheetContent
            side="left"
            className="w-72 sm:w-96"
            aria-label={t('navbarMenuLabel')}
            aria-describedby={undefined}
          >
            <SheetHeader className="mb-4">
              <SheetTitle>{t('navbarMenuLabel')}</SheetTitle>
            </SheetHeader>
            <NavItemsList
              items={navItems}
              variant="mobile"
              onNavigate={() => setIsMenuOpen(false)}
            />
          </SheetContent>
        </Sheet>
      </div>
      <NavItemsList items={navItems} variant="desktop" />
    </nav>
  );
}
