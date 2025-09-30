import { useEffect, useMemo, useState } from 'react';
import { User as UserIcon, Key, UserX, Loader2 } from 'lucide-react';
import { useQueryClient } from '@tanstack/react-query';
import type { AccessProfile, UserDto } from '@photobank/shared';
import {
  getAdminAccessProfilesListQueryKey,
  useAdminAccessProfilesAssignUser,
  useAdminAccessProfilesList,
  useAdminAccessProfilesUnassignUser,
} from '@photobank/shared/api/photobank/admin-access-profiles/admin-access-profiles';

import { Button } from '@/shared/ui/button';
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
  const queryClient = useQueryClient();

  const accessProfilesQueryKey = useMemo(
    () => getAdminAccessProfilesListQueryKey(),
    []
  );

  const {
    data: accessProfilesData,
    isLoading: isAccessProfilesLoading,
    isError: isAccessProfilesError,
    isFetching: isAccessProfilesFetching,
    refetch: refetchAccessProfiles,
  } = useAdminAccessProfilesList();

  const accessProfiles = useMemo<AccessProfile[]>(
    () => accessProfilesData?.data ?? [],
    [accessProfilesData]
  );

  const derivedAssignedProfileIds = useMemo(() => {
    const rawProfiles = (user as {
      accessProfiles?: Array<{ profileId?: number | null; id?: number | null }>;
    }).accessProfiles;

    if (!rawProfiles) {
      return [] as number[];
    }

    return rawProfiles
      .map((profile) => profile?.profileId ?? profile?.id)
      .filter((profileId): profileId is number => typeof profileId === 'number');
  }, [user]);

  const [assignedProfileIds, setAssignedProfileIds] = useState<Set<number>>(
    () => new Set(derivedAssignedProfileIds)
  );
  const [activeProfileId, setActiveProfileId] = useState<number | null>(null);

  useEffect(() => {
    setAssignedProfileIds(new Set(derivedAssignedProfileIds));
  }, [derivedAssignedProfileIds]);

  const assignProfileMutation = useAdminAccessProfilesAssignUser();
  const unassignProfileMutation = useAdminAccessProfilesUnassignUser();

  const userId = user.id ?? undefined;
  const userEmail = user.email ?? 'the user';
  const canManageAccessProfiles = typeof userId === 'string' && userId.length > 0;

  const showAccessProfilesLoading = isAccessProfilesLoading && accessProfiles.length === 0;
  const showAccessProfilesError = isAccessProfilesError && accessProfiles.length === 0;

  const handleAssignProfile = async (profile: AccessProfile) => {
    const profileId = profile.id;

    if (typeof profileId !== 'number') {
      toast({
        title: 'Unable to assign profile',
        description: 'This profile is missing an identifier.',
        variant: 'destructive',
      });
      return;
    }

    if (!canManageAccessProfiles || !userId) {
      toast({
        title: 'Unable to assign profile',
        description: 'This user is missing an identifier. Please refresh and try again.',
        variant: 'destructive',
      });
      return;
    }

    setActiveProfileId(profileId);

    try {
      await assignProfileMutation.mutateAsync({ id: profileId, userId });

      setAssignedProfileIds((prev) => {
        const next = new Set(prev);
        next.add(profileId);
        return next;
      });

      toast({
        title: 'Profile assigned',
        description: `${profile.name ?? `Profile #${profileId}`} assigned to ${userEmail}.`,
      });

      await queryClient.invalidateQueries({ queryKey: accessProfilesQueryKey });
    } catch (error) {
      toast({
        title: 'Failed to assign profile',
        description: 'Something went wrong while assigning the profile. Please try again.',
        variant: 'destructive',
      });
    } finally {
      setActiveProfileId(null);
    }
  };

  const handleUnassignProfile = async (profile: AccessProfile) => {
    const profileId = profile.id;

    if (typeof profileId !== 'number') {
      toast({
        title: 'Unable to unassign profile',
        description: 'This profile is missing an identifier.',
        variant: 'destructive',
      });
      return;
    }

    if (!canManageAccessProfiles || !userId) {
      toast({
        title: 'Unable to unassign profile',
        description: 'This user is missing an identifier. Please refresh and try again.',
        variant: 'destructive',
      });
      return;
    }

    setActiveProfileId(profileId);

    try {
      await unassignProfileMutation.mutateAsync({ id: profileId, userId });

      setAssignedProfileIds((prev) => {
        const next = new Set(prev);
        next.delete(profileId);
        return next;
      });

      toast({
        title: 'Profile unassigned',
        description: `${profile.name ?? `Profile #${profileId}`} removed from ${userEmail}.`,
      });

      await queryClient.invalidateQueries({ queryKey: accessProfilesQueryKey });
    } catch (error) {
      toast({
        title: 'Failed to unassign profile',
        description: 'Something went wrong while unassigning the profile. Please try again.',
        variant: 'destructive',
      });
    } finally {
      setActiveProfileId(null);
    }
  };

  const isAnyMutationRunning =
    assignProfileMutation.isPending || unassignProfileMutation.isPending;

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

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:w-[600px] sm:max-w-[600px] overflow-y-auto p-4 sm:p-6">
        <SheetHeader className="pb-6">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <Avatar className="h-12 w-12">
                <AvatarFallback className="bg-primary/10 text-primary text-lg">
                  {user.email?.slice(0, 2).toUpperCase()}
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

          <TabsContent value="access" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Access Profiles</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                {showAccessProfilesError ? (
                  <div className="flex flex-col items-center justify-center gap-3 py-6 text-center">
                    <p className="text-sm text-destructive">We couldn&apos;t load access profiles.</p>
                    <Button variant="outline" onClick={() => refetchAccessProfiles()} disabled={isAnyMutationRunning}>
                      Try again
                    </Button>
                  </div>
                ) : showAccessProfilesLoading ? (
                  <div className="space-y-3">
                    {Array.from({ length: 3 }).map((_, index) => (
                      <div key={index} className="h-12 rounded-lg bg-muted/60 animate-pulse" />
                    ))}
                  </div>
                ) : accessProfiles.length === 0 ? (
                  <p className="text-sm text-muted-foreground text-center py-4">
                    No access profiles are currently available.
                  </p>
                ) : (
                  <div className="space-y-3">
                    {accessProfiles.map((profile) => {
                      const profileId = profile.id;
                      const isAssigned =
                        typeof profileId === 'number' && assignedProfileIds.has(profileId);
                      const isCurrentProfileMutating =
                        isAnyMutationRunning && activeProfileId === profileId;
                      const actionLabel = isAssigned ? 'Unassign' : 'Assign';
                      const disableButton =
                        !canManageAccessProfiles ||
                        typeof profileId !== 'number' ||
                        isCurrentProfileMutating ||
                        isAnyMutationRunning;

                      return (
                        <div
                          key={profileId ?? profile.name ?? `profile-${actionLabel}`}
                          className="flex flex-col sm:flex-row sm:items-center gap-3 p-4 bg-muted/50 rounded-lg"
                        >
                          <div className="flex items-center gap-3 flex-1">
                            <Badge variant={isAssigned ? 'default' : 'outline'}>
                              {isAssigned ? 'Assigned' : 'Available'}
                            </Badge>
                            <span className="font-medium">{profile.name ?? 'Untitled profile'}</span>
                          </div>
                          <Button
                            variant={isAssigned ? 'outline' : 'default'}
                            className="w-full sm:w-auto h-11"
                            disabled={disableButton}
                            onClick={() =>
                              isAssigned
                                ? handleUnassignProfile(profile)
                                : handleAssignProfile(profile)
                            }
                          >
                            {isCurrentProfileMutating ? (
                              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                            ) : null}
                            {actionLabel}
                          </Button>
                        </div>
                      );
                    })}
                  </div>
                )}
                {isAccessProfilesFetching && accessProfiles.length > 0 ? (
                  <p className="text-xs text-muted-foreground text-center">Refreshing…</p>
                ) : null}
                {!canManageAccessProfiles ? (
                  <p className="text-xs text-muted-foreground text-center">
                    Assignments are unavailable because this user does not have an identifier.
                  </p>
                ) : null}
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </SheetContent>
    </Sheet>
  );
}