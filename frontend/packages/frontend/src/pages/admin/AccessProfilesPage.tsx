import { useState } from 'react';
import { Plus, Search } from 'lucide-react';

import { Button } from '@/shared/ui/button';
import { Input } from '@/shared/ui/input';
import { Card, CardContent } from '@/shared/ui/card';
import { AccessProfilesGrid } from '@/components/admin/AccessProfilesGrid';
import { CreateProfileDialog } from '@/components/admin/CreateProfileDialog';
import { EditProfileDialog } from '@/components/admin/EditProfileDialog';
import { mockAccessProfiles } from '@/data/mockData';
import { AccessProfileUI, AccessProfile } from '@/types/admin';

export default function AccessProfilesPage() {
  const [profiles] = useState<AccessProfileUI[]>(mockAccessProfiles);
  const [searchQuery, setSearchQuery] = useState('');
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [selectedProfile, setSelectedProfile] = useState<AccessProfile | null>(null);

  const filteredProfiles = profiles.filter((profile) =>
    profile.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    profile.description.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const handleEditProfile = (profile: AccessProfileUI) => {
    // Convert AccessProfileUI to AccessProfile format for editing
    const accessProfile: AccessProfile = {
      id: profile.id,
      name: profile.name,
      description: profile.description,
      flags_CanSeeNsfw: profile.flags_CanSeeNsfw || false,
      storages: profile.storages.map((storageName, index) => ({
        profileId: profile.id,
        storageId: index + 1, // Mock mapping - in real app this would be proper lookup
      })),
      personGroups: profile.personGroups.map((groupName, index) => ({
        profileId: profile.id,
        personGroupId: index + 1, // Mock mapping - in real app this would be proper lookup
      })),
      dateRanges: profile.dateRanges.map(range => ({
        profileId: profile.id,
        fromDate: range.from,
        toDate: range.to,
      })),
    };
    
    setSelectedProfile(accessProfile);
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
          className="bg-gradient-primary hover:bg-primary-hover shadow-elevated w-full sm:w-auto"
          size="lg"
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
          </h2>
        </div>
        <AccessProfilesGrid profiles={filteredProfiles} onEditProfile={handleEditProfile} />
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