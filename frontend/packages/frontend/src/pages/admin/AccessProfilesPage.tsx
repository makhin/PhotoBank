import { useMemo, useState } from 'react';
import { Plus, Search } from 'lucide-react';
import type { AccessProfileDto } from '@photobank/shared';
import { useAdminAccessProfilesList } from '@photobank/shared/api/photobank/admin-access-profiles/admin-access-profiles';

import { Button } from '@/shared/ui/button';
import { Input } from '@/shared/ui/input';
import { Card, CardContent } from '@/shared/ui/card';
import { AccessProfilesGrid } from '@/components/admin/AccessProfilesGrid';
import { CreateProfileDialog } from '@/components/admin/CreateProfileDialog';
import { EditProfileDialog } from '@/components/admin/EditProfileDialog';
import { useToast } from '@/hooks/use-toast';

export default function AccessProfilesPage() {
  const { toast } = useToast();
  const [searchQuery, setSearchQuery] = useState('');
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [selectedProfile, setSelectedProfile] = useState<AccessProfileDto | null>(null);

  const {
    data,
    isLoading,
    isFetching,
    isError,
    refetch,
  } = useAdminAccessProfilesList();

  const profiles = useMemo<AccessProfileDto[]>(() => data?.data ?? [], [data]);

  const filteredProfiles = useMemo(() => {
    const normalizedQuery = searchQuery.trim().toLowerCase();

    if (!normalizedQuery) {
      return profiles;
    }

    return profiles.filter((profile) => {
      const name = profile.name?.toLowerCase() ?? '';
      const description = profile.description?.toLowerCase() ?? '';

      return name.includes(normalizedQuery) || description.includes(normalizedQuery);
    });
  }, [profiles, searchQuery]);

  const showLoading = isLoading && profiles.length === 0;
  const showError = isError && profiles.length === 0;
  const isRefreshing = isFetching && profiles.length > 0;

  const handleEditProfile = (profile: AccessProfileDto) => {
    if (!profile.id) {
      toast({
        title: 'Unable to edit profile',
        description: 'This profile is missing an identifier.',
        variant: 'destructive',
      });
      return;
    }

    setSelectedProfile(profile);
    setIsEditDialogOpen(true);
  };

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-foreground">Access Profiles</h1>
          <p className="text-muted-foreground mt-1 text-sm sm:text-base">
            Manage access rules and permissions for different user groups
          </p>
        </div>
        <Button
          onClick={() => setIsCreateDialogOpen(true)}
          className="w-full sm:w-auto"
          size="default"
        >
          <Plus className="w-4 h-4 mr-2" />
          Create Profile
        </Button>
      </div>

      {/* Toolbar */}
      <Card className="shadow-card border-border/50">
        <CardContent className="p-4 sm:p-6">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground w-4 h-4" />
            <Input
              placeholder="Search profiles by name or description..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-10 bg-muted/50 border-border h-10"
            />
          </div>
        </CardContent>
      </Card>

      {/* Profiles Grid */}
      <div>
        <div className="mb-4">
          <h2 className="text-lg sm:text-xl font-semibold text-foreground">
            Profiles ({filteredProfiles.length})
            {isRefreshing && (
              <span className="ml-2 text-sm font-normal text-muted-foreground">Refreshing…</span>
            )}
          </h2>
        </div>

        {showError ? (
          <Card className="shadow-card border-border/50">
            <CardContent className="py-12 flex flex-col items-center justify-center gap-4 text-center">
              <p className="text-muted-foreground">We couldn&apos;t load the access profiles right now.</p>
              <Button variant="outline" onClick={() => refetch()}>
                Try again
              </Button>
            </CardContent>
          </Card>
        ) : showLoading ? (
          <Card className="shadow-card border-border/50">
            <CardContent className="space-y-3 p-6">
              {Array.from({ length: 3 }).map((_, index) => (
                <div key={index} className="h-12 bg-muted/60 animate-pulse rounded-lg" />
              ))}
            </CardContent>
          </Card>
        ) : (
          <AccessProfilesGrid
            profiles={filteredProfiles}
            onEditProfile={handleEditProfile}
            onProfileDeleted={refetch}
          />
        )}
      </div>

      {/* Create Dialog */}
      <CreateProfileDialog
        open={isCreateDialogOpen}
        onOpenChange={setIsCreateDialogOpen} 
      />

      {/* Edit Dialog */}
      <EditProfileDialog
        open={isEditDialogOpen}
        onOpenChange={setIsEditDialogOpen}
        profile={selectedProfile}
      />
    </div>
  );
}