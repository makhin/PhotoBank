import { MoreHorizontal, Edit, Key, Trash2, User as UserIcon } from 'lucide-react';
import type { UserDto } from '@photobank/shared';

import { Button } from '@/shared/ui/button';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/shared/ui/table';
import { Badge } from '@/shared/ui/badge';
import { useToast } from '@/hooks/use-toast';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/shared/ui/dropdown-menu';
import { Avatar, AvatarFallback } from '@/shared/ui/avatar';

interface UsersTableProps {
  users: UserDto[];
  onUserSelect: (user: UserDto) => void;
}

export function UsersTable({ users, onUserSelect }: UsersTableProps) {
  const { toast } = useToast();

  const handleResetPassword = (user: UserDto) => {
    toast({
      title: 'Password Reset',
      description: `Password reset email sent to ${user.email}`,
    });
  };

  const handleDeleteUser = (user: UserDto) => {
    toast({
      title: 'User Deleted',
      description: `${user.email} has been removed`,
      variant: 'destructive',
    });
  };

  const getRoleBadgeVariant = (role: string) => {
    return role === 'Admin' ? 'default' : 'secondary';
  };

  if (users.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-center">
        <UserIcon className="w-12 h-12 text-muted-foreground mb-4" />
        <h3 className="text-lg font-medium text-foreground mb-2">No users found</h3>
        <p className="text-muted-foreground max-w-sm">
          No users match your current search criteria. Try adjusting your filters.
        </p>
      </div>
    );
  }

  return (
    <>
      {/* Desktop Table View */}
      <div className="hidden md:block overflow-x-auto">
        <Table>
          <TableHeader>
            <TableRow className="border-border/50">
              <TableHead className="w-[50px]"></TableHead>
              <TableHead>Email</TableHead>
              <TableHead>Phone</TableHead>
              <TableHead>Roles</TableHead>
              <TableHead>Telegram</TableHead>
              <TableHead className="w-[70px]">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {users.map((user) => (
              <TableRow
                key={user.id}
                className="cursor-pointer hover:bg-muted/50 border-border/50"
                onClick={() => onUserSelect(user)}
              >
                <TableCell>
                  <Avatar className="h-8 w-8">
                    <AvatarFallback className="bg-primary/10 text-primary text-xs">
                      {user.email?.slice(0, 2).toUpperCase()}
                    </AvatarFallback>
                  </Avatar>
                </TableCell>
                <TableCell className="font-medium">{user.email}</TableCell>
                <TableCell className="text-muted-foreground">
                  {user.phoneNumber || '—'}
                </TableCell>
                <TableCell>
                  <div className="flex gap-1">
                    {user.roles?.map((role) => (
                      <Badge key={role} variant={getRoleBadgeVariant(role)}>
                        {role}
                      </Badge>
                    ))}
                  </div>
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {user.telegramUserId ? (
                    <div className="text-sm">
                      <div>ID: {user.telegramUserId}</div>
                      <div className="text-xs">
                        Send: {user.telegramSendTimeUtc || 'Not set'}
                      </div>
                    </div>
                  ) : (
                    '—'
                  )}
                </TableCell>
                <TableCell>
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button
                        variant="ghost"
                        className="h-8 w-8 p-0"
                        onClick={(e) => e.stopPropagation()}
                      >
                        <MoreHorizontal className="h-4 w-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end" className="w-[160px]">
                      <DropdownMenuItem
                        onClick={(e) => {
                          e.stopPropagation();
                          onUserSelect(user);
                        }}
                      >
                        <Edit className="mr-2 h-4 w-4" />
                        Edit
                      </DropdownMenuItem>
                      <DropdownMenuItem
                        onClick={(e) => {
                          e.stopPropagation();
                          handleResetPassword(user);
                        }}
                      >
                        <Key className="mr-2 h-4 w-4" />
                        Reset Password
                      </DropdownMenuItem>
                      <DropdownMenuItem
                        onClick={(e) => {
                          e.stopPropagation();
                          handleDeleteUser(user);
                        }}
                        className="text-destructive focus:text-destructive"
                      >
                        <Trash2 className="mr-2 h-4 w-4" />
                        Delete
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      {/* Mobile Card View */}
      <div className="md:hidden space-y-3">
        {users.map((user) => (
          <div
            key={user.id}
            className="bg-card rounded-lg border border-border p-4 shadow-card cursor-pointer hover:bg-muted/50 transition-colors"
            role="button"
            tabIndex={0}
            onClick={() => onUserSelect(user)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                onUserSelect(user);
              }
            }}
          >
            <div className="flex items-start justify-between mb-3">
              <div className="flex items-center gap-3">
                <Avatar className="h-10 w-10">
                  <AvatarFallback className="bg-primary/10 text-primary text-sm">
                    {user.email?.slice(0, 2).toUpperCase()}
                  </AvatarFallback>
                </Avatar>
                <div className="flex-1 min-w-0">
                  <div className="font-medium text-foreground truncate">{user.email}</div>
                  <div className="text-sm text-muted-foreground">
                    {user.phoneNumber || 'No phone'}
                  </div>
                </div>
              </div>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-8 w-8 p-0"
                    onClick={(e) => e.stopPropagation()}
                  >
                    <MoreHorizontal className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-[160px]">
                  <DropdownMenuItem
                    onClick={(e) => {
                      e.stopPropagation();
                      onUserSelect(user);
                    }}
                  >
                    <Edit className="mr-2 h-4 w-4" />
                    Edit
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onClick={(e) => {
                      e.stopPropagation();
                      handleResetPassword(user);
                    }}
                  >
                    <Key className="mr-2 h-4 w-4" />
                    Reset Password
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onClick={(e) => {
                      e.stopPropagation();
                      handleDeleteUser(user);
                    }}
                    className="text-destructive focus:text-destructive"
                  >
                    <Trash2 className="mr-2 h-4 w-4" />
                    Delete
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>

            <div className="space-y-2">
              <div className="flex flex-wrap gap-1">
                {user.roles?.map((role) => (
                  <Badge key={role} variant={getRoleBadgeVariant(role)} className="text-xs">
                    {role}
                  </Badge>
                ))}
              </div>

              {user.telegramUserId && (
                <div className="text-sm text-muted-foreground">
                  <div>Telegram ID: {user.telegramUserId}</div>
                  <div className="text-xs">Send: {user.telegramSendTimeUtc || 'Not set'}</div>
                </div>
              )}

            </div>
          </div>
        ))}
      </div>
    </>
  );
}
