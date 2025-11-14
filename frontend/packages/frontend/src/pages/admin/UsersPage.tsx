import { useEffect, useMemo, useState } from 'react';
import { Filter, Plus, Search } from 'lucide-react';
import type { UserDto } from '@photobank/shared';
import { useUsersGetAll } from '@photobank/shared/api/photobank';

import { Button } from '@/shared/ui/button';
import { Input } from '@/shared/ui/input';
import { Card, CardContent } from '@/shared/ui/card';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/ui/select';
import { UsersTable } from '@/components/admin/UsersTable';
import { CreateUserDialog } from '@/components/admin/CreateUserDialog';
import { UserDetailsDrawer } from '@/components/admin/UserDetailsDrawer';

export default function UsersPage() {
  const [searchQuery, setSearchQuery] = useState('');
  const [roleFilter, setRoleFilter] = useState<string>('all');
  const [selectedUser, setSelectedUser] = useState<UserDto | null>(null);
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [isUserDrawerOpen, setIsUserDrawerOpen] = useState(false);

  const { data, isLoading, isFetching, isError, refetch } = useUsersGetAll();
  const users = useMemo(() => data?.data ?? [], [data]);

  useEffect(() => {
    if (!selectedUser) {
      return;
    }

    const stillExists = users.some((user) => user.id === selectedUser.id);
    if (!stillExists) {
      setSelectedUser(null);
      setIsUserDrawerOpen(false);
    }
  }, [users, selectedUser]);

  const filteredUsers = useMemo(() => {
    const normalizedQuery = searchQuery.trim().toLowerCase();
    const isQueryEmpty = normalizedQuery.length === 0;

    if (isQueryEmpty && roleFilter === 'all') {
      return users;
    }

    return users.filter((user) => {
      const matchesSearch =
        isQueryEmpty ||
        (user.email?.toLowerCase().includes(normalizedQuery) ?? false) ||
        (typeof user.phoneNumber === 'string' &&
          user.phoneNumber.toLowerCase().includes(normalizedQuery));

      const matchesRole =
        roleFilter === 'all' || (user.roles?.includes(roleFilter) ?? false);

      return matchesSearch && matchesRole;
    });
  }, [users, searchQuery, roleFilter]);

  const hasUsersLoaded = users.length > 0;
  const showLoading = isLoading && !hasUsersLoaded;
  const showError = isError && !hasUsersLoaded;

  const handleUserSelect = (user: UserDto) => {
    setSelectedUser(user);
    setIsUserDrawerOpen(true);
  };

  const handleUserUpdated = (updatedUser: UserDto) => {
    setSelectedUser(updatedUser);
  };

  const renderUsersSection = () => {
    if (showLoading) {
      return (
        <Card className="shadow-card border-border/50">
          <CardContent className="p-6">
            <div className="flex items-center justify-center text-sm text-muted-foreground">
              Loading users…
            </div>
          </CardContent>
        </Card>
      );
    }

    if (showError) {
      return (
        <Card className="shadow-card border-border/50">
          <CardContent className="p-6 flex flex-col items-center gap-3 text-center">
            <p className="text-sm text-destructive">Failed to load users.</p>
            <Button variant="outline" onClick={() => refetch()}>
              Try again
            </Button>
          </CardContent>
        </Card>
      );
    }

    return (
      <>
        <Card className="shadow-card border-border/50 hidden md:block">
          <CardContent className="p-0">
            <UsersTable users={filteredUsers} onUserSelect={handleUserSelect} />
          </CardContent>
        </Card>

        <div className="md:hidden">
          <UsersTable users={filteredUsers} onUserSelect={handleUserSelect} />
        </div>
      </>
    );
  };

  const usersCountLabel = showLoading ? null : ` (${filteredUsers.length})`;

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-foreground">Users Management</h1>
          <p className="text-muted-foreground mt-1 text-sm sm:text-base">
            Manage user accounts, roles, and permissions
          </p>
        </div>
        <Button
          onClick={() => setIsCreateDialogOpen(true)}
          className="w-full sm:w-auto"
          size="default"
        >
          <Plus className="w-4 h-4 mr-2" />
          Create User
        </Button>
      </div>

      <Card className="shadow-card border-border/50">
        <CardContent className="p-4 sm:p-6">
          <div className="flex flex-col gap-3 sm:flex-row sm:gap-4">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground w-4 h-4" />
              <Input
                placeholder="Search by email or phone..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-10 bg-muted/50 border-border h-10"
              />
            </div>
            <div className="flex items-center gap-2">
              <Filter className="w-4 h-4 text-muted-foreground hidden sm:block" />
              <Select value={roleFilter} onValueChange={setRoleFilter}>
                <SelectTrigger className="w-full sm:w-[180px] h-10">
                  <SelectValue placeholder="Filter by role" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Roles</SelectItem>
                  <SelectItem value="Admin">Admin</SelectItem>
                  <SelectItem value="User">User</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      <div>
        <div className="mb-4 flex flex-col gap-1 sm:flex-row sm:items-center sm:justify-between">
          <h2 className="text-lg sm:text-xl font-semibold text-foreground">
            Users{usersCountLabel}
          </h2>
          {isFetching && !showLoading ? (
            <span className="text-xs text-muted-foreground">Refreshing…</span>
          ) : null}
        </div>

        {renderUsersSection()}
      </div>

      <CreateUserDialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen} />

      {selectedUser && (
        <UserDetailsDrawer
          user={selectedUser}
          open={isUserDrawerOpen}
          onOpenChange={setIsUserDrawerOpen}
          onUserUpdated={handleUserUpdated}
        />
      )}
    </div>
  );
}