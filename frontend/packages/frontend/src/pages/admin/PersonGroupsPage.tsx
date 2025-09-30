import { useMemo, useState } from 'react';
import { Plus, MoreVertical, Users, Edit, Trash2 } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import type { PersonGroupDto } from '@photobank/shared';
import { useQueryClient } from '@tanstack/react-query';
import {
  getPersonGroupsGetAllQueryKey,
  usePersonGroupsDelete,
  usePersonGroupsGetAll,
} from '@photobank/shared/api/photobank/person-groups/person-groups';

import { Button } from '@/shared/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { Badge } from '@/shared/ui/badge';
import { CreatePersonGroupDialog } from '@/components/admin/CreatePersonGroupDialog';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/shared/ui/dropdown-menu';
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from '@/shared/ui/alert-dialog';
import { useToast } from '@/hooks/use-toast';

export default function PersonGroupsPage() {
  const navigate = useNavigate();
  const { toast } = useToast();
  const queryClient = useQueryClient();
  const personGroupsQueryKey = useMemo(() => getPersonGroupsGetAllQueryKey(), []);
  const {
    data,
    isLoading,
    isError,
    isFetching,
    refetch,
  } = usePersonGroupsGetAll();
  const groups = useMemo<PersonGroupDto[]>(() => data?.data ?? [], [data]);
  const hasGroupsLoaded = groups.length > 0;
  const showLoading = isLoading && !hasGroupsLoaded;
  const showError = isError && !hasGroupsLoaded;
  const isRefreshing = isFetching && !showLoading;
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [selectedGroup, setSelectedGroup] = useState<PersonGroupDto | null>(null);

  const deletePersonGroupMutation = usePersonGroupsDelete({
    mutation: {
      onSuccess: async (_, variables) => {
        await queryClient.invalidateQueries({ queryKey: personGroupsQueryKey });

        const deletedGroupName =
          selectedGroup?.id === variables.groupId
            ? selectedGroup.name
            : groups.find(group => group.id === variables.groupId)?.name;

        toast({
          title: 'Group deleted',
          description: deletedGroupName
            ? `${deletedGroupName} has been deleted successfully.`
            : 'Group has been deleted successfully.',
        });
      },
      onError: () => {
        toast({
          title: 'Failed to delete group',
          description: 'Something went wrong while deleting the group. Please try again.',
          variant: 'destructive',
        });
      },
      onSettled: () => {
        setShowDeleteDialog(false);
        setSelectedGroup(null);
      },
    },
  });

  const handleDeleteGroup = (group: PersonGroupDto) => {
    setSelectedGroup(group);
    setShowDeleteDialog(true);
  };

  const confirmDelete = async () => {
    if (!selectedGroup) return;

    try {
      await deletePersonGroupMutation.mutateAsync({ groupId: selectedGroup.id });
    } catch {
      // Error handling is managed by the mutation callbacks.
    }
  };

  const handleCreateGroupSuccess = (group: PersonGroupDto) => {
    toast({
      title: 'Group created',
      description: `${group.name} has been created successfully.`,
    });
  };

  const handleCreateGroupError = () => {
    toast({
      title: 'Failed to create group',
      description: 'Something went wrong while creating the group. Please try again.',
      variant: 'destructive',
    });
  };

  if (showError) {
    return (
      <div className="space-y-6">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <h1 className="text-2xl font-semibold tracking-tight">Person Groups</h1>
            <p className="text-muted-foreground">
              Manage person groups and their members
            </p>
          </div>
          <Button onClick={() => setShowCreateDialog(true)} size="default" className="w-full sm:w-auto">
            <Plus className="h-4 w-4" />
            Create Group
          </Button>
        </div>

        <Card>
          <CardContent className="py-12 text-center space-y-4">
            <p className="text-muted-foreground">We couldn&apos;t load the person groups right now.</p>
            <Button onClick={() => refetch()} variant="outline">
              Try again
            </Button>
          </CardContent>
        </Card>

        <CreatePersonGroupDialog
          open={showCreateDialog}
          onOpenChange={setShowCreateDialog}
          onSuccess={handleCreateGroupSuccess}
          onError={handleCreateGroupError}
        />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Person Groups</h1>
          <p className="text-muted-foreground">
            Manage person groups and their members
          </p>
        </div>
        <Button onClick={() => setShowCreateDialog(true)} size="default" className="w-full sm:w-auto">
          <Plus className="h-4 w-4" />
          Create Group
        </Button>
      </div>

      {/* Mobile Card Layout */}
      <div className="grid gap-4 md:hidden">
        {showLoading ? (
          Array.from({ length: 3 }).map((_, index) => (
            <Card key={index} className="p-4 animate-pulse">
              <div className="h-5 w-1/2 bg-muted rounded mb-3" />
              <div className="h-4 w-1/3 bg-muted rounded" />
            </Card>
          ))
        ) : (
          groups.map((group) => (
            <Card key={group.id} className="p-4">
              <div className="flex items-start justify-between mb-3">
                <div className="flex-1">
                  <h3 className="font-medium truncate">{group.name}</h3>
                  <p className="text-sm text-muted-foreground mt-1 line-clamp-2">
                    {group.persons?.length ?? 0} persons
                  </p>
                </div>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon" className="h-8 w-8 ml-2">
                      <MoreVertical className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem onClick={() => navigate(`/admin/person-groups/${group.id}`)}>
                      <Edit className="h-4 w-4" />
                      Edit Group
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      onClick={() => handleDeleteGroup(group)}
                      className="text-destructive"
                    >
                      <Trash2 className="h-4 w-4" />
                      Delete
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>

              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <Users className="h-4 w-4 text-muted-foreground" />
                  <Badge variant="secondary">
                    {group.persons?.length ?? 0} persons
                  </Badge>
                </div>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => navigate(`/admin/person-groups/${group.id}`)}
                >
                  Manage
                </Button>
              </div>
            </Card>
          ))
        )}
      </div>

      {/* Desktop Table Layout */}
      <div className="hidden md:block">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <span>All Groups</span>
              {isRefreshing && (
                <span className="text-xs font-medium text-muted-foreground">Refreshing…</span>
              )}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {showLoading ? (
                Array.from({ length: 4 }).map((_, index) => (
                  <div key={index} className="flex items-center justify-between p-4 border rounded-lg animate-pulse">
                    <div className="flex-1 space-y-2">
                      <div className="h-5 w-1/4 bg-muted rounded" />
                      <div className="h-4 w-1/5 bg-muted rounded" />
                    </div>
                    <div className="h-8 w-24 bg-muted rounded" />
                  </div>
                ))
              ) : (
                groups.map((group) => (
                  <div key={group.id} className="flex items-center justify-between p-4 border rounded-lg">
                    <div className="flex-1">
                      <div className="flex items-center gap-3">
                        <div>
                          <h3 className="font-medium">{group.name}</h3>
                          <p className="text-sm text-muted-foreground">{group.persons?.length ?? 0} persons</p>
                        </div>
                      </div>
                    </div>

                    <div className="flex items-center gap-4">
                      <Badge variant="secondary" className="flex items-center gap-1">
                        <Users className="h-3 w-3" />
                        {group.persons?.length ?? 0} persons
                      </Badge>

                      <div className="flex items-center gap-2">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => navigate(`/admin/person-groups/${group.id}`)}
                        >
                          <Edit className="h-4 w-4" />
                          Edit
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleDeleteGroup(group)}
                          className="text-destructive hover:text-destructive"
                        >
                          <Trash2 className="h-4 w-4" />
                          Delete
                        </Button>
                      </div>
                    </div>
                  </div>
                ))
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {!showLoading && groups.length === 0 && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Users className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-medium mb-2">No groups found</h3>
            <p className="text-muted-foreground text-center mb-4">
              Get started by creating your first person group
            </p>
            <Button onClick={() => setShowCreateDialog(true)}>
              <Plus className="h-4 w-4" />
              Create Group
            </Button>
          </CardContent>
        </Card>
      )}

      <CreatePersonGroupDialog
        open={showCreateDialog}
        onOpenChange={setShowCreateDialog}
        onSuccess={handleCreateGroupSuccess}
        onError={handleCreateGroupError}
      />

      <AlertDialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Group</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete &ldquo;{selectedGroup?.name}&rdquo;? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={confirmDelete}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              disabled={deletePersonGroupMutation.isPending}
            >
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
