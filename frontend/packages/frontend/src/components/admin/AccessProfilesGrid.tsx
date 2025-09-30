import { Shield, Calendar, Database, Users, Edit, Trash2, MoreHorizontal } from 'lucide-react';
import type { AccessProfile } from '@photobank/shared';

import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { Badge } from '@/shared/ui/badge';
import { Button } from '@/shared/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/shared/ui/dropdown-menu';
import { useToast } from '@/hooks/use-toast';

interface AccessProfilesGridProps {
  profiles: AccessProfile[];
  onEditProfile: (profile: AccessProfile) => void;
}

export function AccessProfilesGrid({ profiles, onEditProfile }: AccessProfilesGridProps) {
  const { toast } = useToast();

  const handleEdit = (profile: AccessProfile) => {
    onEditProfile(profile);
  };

  const handleDelete = (profile: AccessProfile) => {
    toast({
      title: 'Profile Deleted',
      description: `${profile.name} has been removed`,
      variant: 'destructive',
    });
  };

  if (profiles.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-center">
        <Shield className="w-12 h-12 text-muted-foreground mb-4" />
        <h3 className="text-lg font-medium text-foreground mb-2">No profiles found</h3>
        <p className="text-muted-foreground max-w-sm">
          No access profiles match your current search criteria. Try adjusting your search.
        </p>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-4 sm:gap-6">
      {profiles.map((profile) => (
        <Card key={profile.id} className="shadow-card border-border/50 hover:shadow-elevated transition-shadow">
          <CardHeader className="pb-3 p-4 sm:p-6">
            <div className="flex items-start justify-between gap-2">
              <div className="flex items-center gap-2 min-w-0 flex-1">
                <Shield className="w-5 h-5 text-primary shrink-0" />
                <CardTitle className="text-base sm:text-lg truncate">{profile.name}</CardTitle>
              </div>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" className="h-8 w-8 p-0 shrink-0">
                    <MoreHorizontal className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-[160px]">
                  <DropdownMenuItem onClick={() => handleEdit(profile)}>
                    <Edit className="mr-2 h-4 w-4" />
                    Edit
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onClick={() => handleDelete(profile)}
                    className="text-destructive focus:text-destructive"
                  >
                    <Trash2 className="mr-2 h-4 w-4" />
                    Delete
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
            <p className="text-sm text-muted-foreground mt-2">{profile.description}</p>
          </CardHeader>
          
          <CardContent className="space-y-4 p-4 sm:p-6 sm:pt-0">
            {/* Rules Summary */}
            <div className="space-y-3">
              <div className="flex flex-col sm:flex-row sm:items-center gap-2 text-sm">
                <div className="flex items-center gap-2">
                  <Database className="w-4 h-4 text-muted-foreground" />
                  <span className="font-medium">{profile.storages.length} Storages</span>
                </div>
                <div className="flex flex-wrap gap-1">
                  {profile.storages.slice(0, 2).map((storage) => (
                    <Badge key={storage} variant="outline" className="text-xs">
                      {storage}
                    </Badge>
                  ))}
                  {profile.storages.length > 2 && (
                    <Badge variant="outline" className="text-xs">
                      +{profile.storages.length - 2}
                    </Badge>
                  )}
                </div>
              </div>

              <div className="flex flex-col sm:flex-row sm:items-center gap-2 text-sm">
                <div className="flex items-center gap-2">
                  <Users className="w-4 h-4 text-muted-foreground" />
                  <span className="font-medium">{profile.personGroups.length} Person Groups</span>
                </div>
                <div className="flex flex-wrap gap-1">
                  {profile.personGroups.slice(0, 2).map((group) => (
                    <Badge key={group} variant="outline" className="text-xs">
                      {group}
                    </Badge>
                  ))}
                  {profile.personGroups.length > 2 && (
                    <Badge variant="outline" className="text-xs">
                      +{profile.personGroups.length - 2}
                    </Badge>
                  )}
                </div>
              </div>

              <div className="flex flex-col sm:flex-row sm:items-center gap-2 text-sm">
                <div className="flex items-center gap-2">
                  <Calendar className="w-4 h-4 text-muted-foreground" />
                  <span className="font-medium">{profile.dateRanges.length} Date Ranges</span>
                </div>
                {profile.dateRanges.length > 0 && (
                  <Badge variant="outline" className="text-xs">
                    {profile.dateRanges[0].from} - {profile.dateRanges[0].to}
                  </Badge>
                )}
              </div>
            </div>

            {/* Assignments */}
            <div className="pt-3 border-t border-border/50">
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 text-sm">
                <span className="text-muted-foreground">Assigned to:</span>
                <div className="flex flex-col sm:flex-row sm:items-center gap-2">
                  <span className="font-medium">{profile.assignedUsersCount} users</span>
                  <div className="flex flex-wrap gap-1">
                    {profile.assignedRoles.map((role) => (
                      <Badge key={role} variant="secondary" className="text-xs">
                        {role}
                      </Badge>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}