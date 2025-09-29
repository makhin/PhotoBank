import { useState } from 'react';
import { Plus, MoreVertical, Users, Edit, Trash2 } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

import { Button } from '@/shared/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { Badge } from '@/shared/ui/badge';
import { CreatePersonGroupDialog } from '@/components/admin/CreatePersonGroupDialog';

import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/shared/ui/dropdown-menu';
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from '@/shared/ui/alert-dialog';
import { mockPersonGroupsWithMembers } from '@/data/mockData';
import { PersonGroupWithMembers } from '@/types/admin';
import { useToast } from '@/hooks/use-toast';

export default function PersonGroupsPage() {
  const navigate = useNavigate();
  const { toast } = useToast();
  const [groups, setGroups] = useState<PersonGroupWithMembers[]>(mockPersonGroupsWithMembers);
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [selectedGroup, setSelectedGroup] = useState<PersonGroupWithMembers | null>(null);

  const handleDeleteGroup = (group: PersonGroupWithMembers) => {
    setSelectedGroup(group);
    setShowDeleteDialog(true);
  };

  const confirmDelete = () => {
    if (selectedGroup) {
      setGroups(groups.filter(g => g.id !== selectedGroup.id));
      toast({
        title: "Group deleted",
        description: `${selectedGroup.name} has been deleted successfully.`,
      });
    }
    setShowDeleteDialog(false);
    setSelectedGroup(null);
  };

  const handleCreateGroup = (group: PersonGroupWithMembers) => {
    setGroups([...groups, group]);
    setShowCreateDialog(false);
    toast({
      title: "Group created",
      description: `${group.name} has been created successfully.`,
    });
  };

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
        {groups.map((group) => (
          <Card key={group.id} className="p-4">
            <div className="flex items-start justify-between mb-3">
              <div className="flex-1">
                <h3 className="font-medium truncate">{group.name}</h3>
                <p className="text-sm text-muted-foreground mt-1 line-clamp-2">
                  {group.members.length} members
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
                  {group.members.length} members
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
        ))}
      </div>

      {/* Desktop Table Layout */}
      <div className="hidden md:block">
        <Card>
          <CardHeader>
            <CardTitle>All Groups</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {groups.map((group) => (
                <div key={group.id} className="flex items-center justify-between p-4 border rounded-lg">
                  <div className="flex-1">
                    <div className="flex items-center gap-3">
                      <div>
                        <h3 className="font-medium">{group.name}</h3>
                        <p className="text-sm text-muted-foreground">{group.members.length} members</p>
                      </div>
                    </div>
                  </div>
                  
                  <div className="flex items-center gap-4">
                    <Badge variant="secondary" className="flex items-center gap-1">
                      <Users className="h-3 w-3" />
                      {group.members.length} members
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
              ))}
            </div>
          </CardContent>
        </Card>
      </div>

      {groups.length === 0 && (
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
        onCreateGroup={handleCreateGroup}
      />

      <AlertDialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Group</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete "{selectedGroup?.name}"? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={confirmDelete} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}