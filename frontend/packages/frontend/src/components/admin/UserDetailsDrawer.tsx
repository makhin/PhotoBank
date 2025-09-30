import { useState } from 'react';
import { format } from 'date-fns';
import { User as UserIcon, Key, UserX, Plus, Trash2 } from 'lucide-react';
import type { UserDto } from '@photobank/shared';

import { Button } from '@/shared/ui/button';
import { Input } from '@/shared/ui/input';
import { Label } from '@/shared/ui/label';
import { Badge } from '@/shared/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/ui/tabs';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { Avatar, AvatarFallback } from '@/shared/ui/avatar';
import { Separator } from '@/shared/ui/separator';
import { useToast } from '@/hooks/use-toast';

import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '@/shared/ui/sheet';


interface UserDetailsDrawerProps {
  user: UserDto;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function UserDetailsDrawer({ user, open, onOpenChange }: UserDetailsDrawerProps) {
  const { toast } = useToast();
  const [newClaimKey, setNewClaimKey] = useState('');
  const [newClaimValue, setNewClaimValue] = useState('');

  const handleResetPassword = () => {
    toast({
      title: 'Password Reset',
      description: `Password reset email sent to ${user.email}`,
    });
  };

  const handleDisableUser = () => {
    toast({
      title: 'User Disabled',
      description: `${user.email} has been disabled`,
      variant: 'destructive',
    });
  };

  const handleAddClaim = () => {
    if (newClaimKey && newClaimValue) {
      toast({
        title: 'Claim Added',
        description: `Added ${newClaimKey}: ${newClaimValue}`,
      });
      setNewClaimKey('');
      setNewClaimValue('');
    }
  };

  const handleRemoveClaim = (key: string) => {
    toast({
      title: 'Claim Removed',
      description: `Removed claim: ${key}`,
    });
  };

  const mockAccessProfiles = [
    { id: 1, name: 'Editors – Summer', assigned: true },
    { id: 2, name: 'Admin Full Access', assigned: false },
    { id: 3, name: 'Family Photos Only', assigned: true },
  ];

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:w-[600px] sm:max-w-[600px] overflow-y-auto p-4 sm:p-6">
        <SheetHeader className="pb-6">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <Avatar className="h-12 w-12">
                <AvatarFallback className="bg-primary/10 text-primary text-lg">
                  {user.email.slice(0, 2).toUpperCase()}
                </AvatarFallback>
              </Avatar>
              <div>
                <SheetTitle className="text-xl">{user.email}</SheetTitle>
                <div className="flex gap-1 mt-1">
                  {user.roles?.map((role) => (
                    <Badge key={role} variant={role === 'Administrator' ? 'default' : 'secondary'}>
                      {role}
                    </Badge>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </SheetHeader>

        <Tabs defaultValue="profile" className="space-y-4">
          <TabsList className="grid w-full grid-cols-3 h-12">
            <TabsTrigger value="profile" className="text-xs sm:text-sm">Profile</TabsTrigger>
            <TabsTrigger value="claims" className="text-xs sm:text-sm">Claims</TabsTrigger>
            <TabsTrigger value="access" className="text-xs sm:text-sm">Access</TabsTrigger>
          </TabsList>

          <TabsContent value="profile" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="text-lg flex items-center gap-2">
                  <UserIcon className="w-5 h-5" />
                  Basic Information
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <Label className="text-sm font-medium text-muted-foreground">Email</Label>
                    <p className="text-sm break-all">{user.email}</p>
                  </div>
                  <div>
                    <Label className="text-sm font-medium text-muted-foreground">Phone</Label>
                    <p className="text-sm">{user.phoneNumber || 'Not provided'}</p>
                  </div>
                  <div>
                    <Label className="text-sm font-medium text-muted-foreground">Created</Label>
                    <p className="text-sm">{user.createdAt ? format(new Date(user.createdAt), 'MMM d, yyyy') : 'Not available'}</p>
                  </div>
                  <div>
                    <Label className="text-sm font-medium text-muted-foreground">Last Login</Label>
                    <p className="text-sm">
                      {user.lastLogin ? format(new Date(user.lastLogin), 'MMM d, yyyy HH:mm') : 'Never'}
                    </p>
                  </div>
                </div>

                {user.telegramUserId && (
                  <>
                    <Separator />
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                      <div>
                        <Label className="text-sm font-medium text-muted-foreground">Telegram ID</Label>
                        <p className="text-sm">{user.telegramUserId}</p>
                      </div>
                      <div>
                        <Label className="text-sm font-medium text-muted-foreground">Send Time</Label>
                        <p className="text-sm">{user.telegramSendTimeUtc || 'Not set'}</p>
                      </div>
                    </div>
                  </>
                )}

                <Separator />
                <div className="flex flex-col sm:flex-row gap-3">
                  <Button onClick={handleResetPassword} variant="outline" className="w-full sm:w-auto h-11">
                    <Key className="w-4 h-4 mr-2" />
                    Reset Password
                  </Button>
                  <Button onClick={handleDisableUser} variant="outline" className="w-full sm:w-auto h-11">
                    <UserX className="w-4 h-4 mr-2" />
                    Disable User
                  </Button>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="claims" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Custom Claims</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                {user.claims && user.claims.length > 0 ? (
                  <div className="space-y-2">
                    {user.claims.map((claim) => (
                      <div key={claim.type} className="flex items-center justify-between p-3 bg-muted/50 rounded-lg">
                        <div>
                          <span className="font-medium">{claim.type}:</span>
                          <span className="ml-2 text-muted-foreground">{claim.value}</span>
                        </div>
                        <Button
                          onClick={() => handleRemoveClaim(claim.type)}
                          variant="ghost"
                          size="sm"
                          className="text-destructive hover:text-destructive"
                        >
                          <Trash2 className="w-4 h-4" />
                        </Button>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-muted-foreground text-center py-4">No custom claims defined</p>
                )}

                <Separator />
                <div className="space-y-3">
                  <Label className="text-sm font-medium">Add New Claim</Label>
                  <div className="flex flex-col sm:flex-row gap-3">
                    <Input
                      placeholder="Key"
                      value={newClaimKey}
                      onChange={(e) => setNewClaimKey(e.target.value)}
                      className="h-11"
                    />
                    <Input
                      placeholder="Value"
                      value={newClaimValue}
                      onChange={(e) => setNewClaimValue(e.target.value)}
                      className="h-11"
                    />
                    <Button onClick={handleAddClaim} className="w-full sm:w-auto h-11">
                      <Plus className="w-4 h-4 mr-2 sm:mr-0" />
                      <span className="sm:hidden">Add Claim</span>
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="access" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Access Profiles</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                {mockAccessProfiles.map((profile) => (
                  <div key={profile.id} className="flex flex-col sm:flex-row sm:items-center gap-3 p-4 bg-muted/50 rounded-lg">
                    <div className="flex items-center gap-3 flex-1">
                      <Badge variant={profile.assigned ? 'default' : 'outline'}>
                        {profile.assigned ? 'Assigned' : 'Available'}
                      </Badge>
                      <span className="font-medium">{profile.name}</span>
                    </div>
                    <Button
                      variant={profile.assigned ? 'outline' : 'default'}
                      className="w-full sm:w-auto h-11"
                      onClick={() => {
                        toast({
                          title: profile.assigned ? 'Profile Unassigned' : 'Profile Assigned',
                          description: `${profile.name} ${profile.assigned ? 'removed from' : 'assigned to'} ${user.email}`,
                        });
                      }}
                    >
                      {profile.assigned ? 'Unassign' : 'Assign'}
                    </Button>
                  </div>
                ))}
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </SheetContent>
    </Sheet>
  );
}