import { useState } from 'react';
import { Plus, Search, Filter } from 'lucide-react';
import type { UserDto } from '@photobank/shared';

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
  const [users] = useState<UserDto[]>(mockUsers);
  const [searchQuery, setSearchQuery] = useState('');
  const [roleFilter, setRoleFilter] = useState<string>('all');
  const [selectedUser, setSelectedUser] = useState<UserDto | null>(null);
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [isUserDrawerOpen, setIsUserDrawerOpen] = useState(false);

  const filteredUsers = users.filter((user) => {
    const matchesSearch = 
      user.email?.toLowerCase().includes(searchQuery.toLowerCase()) ||
      (user.phoneNumber && user.phoneNumber.includes(searchQuery));
    
    const matchesRole = 
      roleFilter === 'all' || 
      (user.roles && user.roles.includes(roleFilter));
    
    return matchesSearch && matchesRole;
  });

  const handleUserSelect = (user: UserDto) => {
    setSelectedUser(user);
    setIsUserDrawerOpen(true);
  };

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-foreground">Users Management</h1>
          <p className="text-muted-foreground mt-1 text-sm sm:text-base">
            Manage user accounts, roles, and permissions
          </p>
        </div>
        <Button 
          onClick={() => setIsCreateDialogOpen(true)}
          className="bg-gradient-primary hover:bg-primary-hover shadow-elevated w-full sm:w-auto"
          size="lg"
        >
          <Plus className="w-4 h-4 mr-2" />
          Create User
        </Button>
      </div>

      {/* Toolbar */}
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
                  <SelectItem value="Administrator">Administrator</SelectItem>
                  <SelectItem value="User">User</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Users Table */}
      <div>
        <div className="mb-4">
          <h2 className="text-lg sm:text-xl font-semibold text-foreground">
            Users ({filteredUsers.length})
          </h2>
        </div>
        <Card className="shadow-card border-border/50 hidden md:block">
          <CardContent className="p-0">
            <UsersTable 
              users={filteredUsers} 
              onUserSelect={handleUserSelect}
            />
          </CardContent>
        </Card>
        
        {/* Mobile view - direct cards without card wrapper */}
        <div className="md:hidden">
          <UsersTable 
            users={filteredUsers} 
            onUserSelect={handleUserSelect}
          />
        </div>
      </div>

      {/* Dialogs and Drawers */}
      <CreateUserDialog 
        open={isCreateDialogOpen} 
        onOpenChange={setIsCreateDialogOpen} 
      />
      
      {selectedUser && (
        <UserDetailsDrawer
          user={selectedUser}
          open={isUserDrawerOpen}
          onOpenChange={setIsUserDrawerOpen}
        />
      )}
    </div>
  );
}