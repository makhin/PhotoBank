import { useMemo, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Search, UserPlus, UserMinus, Users } from 'lucide-react';
import type { PersonDto, PersonGroupDto } from '@photobank/shared';
import { useQueryClient } from '@tanstack/react-query';
import {
  getPersonGroupsGetAllQueryKey,
  usePersonGroupsAddPerson,
  usePersonGroupsGetAll,
  usePersonGroupsRemovePerson,
} from '@photobank/shared/api/photobank/person-groups/person-groups';
import { usePersonsGetAll } from '@photobank/shared/api/photobank';

import { Button } from '@/shared/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { Badge } from '@/shared/ui/badge';
import { Input } from '@/shared/ui/input';

import { Avatar, AvatarFallback } from '@/shared/ui/avatar';
import { useToast } from '@/hooks/use-toast';

export default function EditPersonGroupPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { toast } = useToast();
  const queryClient = useQueryClient();
  const groupId = id ? Number(id) : NaN;
  const isValidGroupId = Number.isFinite(groupId);
  const personGroupsQueryKey = useMemo(() => getPersonGroupsGetAllQueryKey(), []);
  const {
    data: group,
    isLoading: isGroupLoading,
    isError: isGroupError,
    isFetching: isGroupFetching,
    refetch: refetchGroups,
  } = usePersonGroupsGetAll<PersonGroupDto | undefined>({
    query: {
      enabled: isValidGroupId,
      select: (response) => response.data.find((g) => g.id === groupId),
    },
  });
  const {
    data: personsResponse,
    isLoading: isPersonsLoading,
    isError: isPersonsError,
    isFetching: isPersonsFetching,
    refetch: refetchPersons,
  } = usePersonsGetAll();
  const [searchTerm, setSearchTerm] = useState('');

  const persons = useMemo<PersonDto[]>(() => personsResponse?.data ?? [], [personsResponse]);
  const groupMembers = useMemo<PersonDto[]>(() => (group?.persons ?? []) as PersonDto[], [group]);
  const memberIds = useMemo(() => new Set(groupMembers.map((person) => person.id)), [groupMembers]);
  const availablePersons = useMemo(
    () => persons.filter((person) => !memberIds.has(person.id)),
    [persons, memberIds]
  );

  const normalizedSearch = searchTerm.trim().toLowerCase();
  const filteredAvailablePersons = useMemo(
    () =>
      normalizedSearch
        ? availablePersons.filter((person) =>
            person.name.toLowerCase().includes(normalizedSearch)
          )
        : availablePersons,
    [availablePersons, normalizedSearch]
  );
  const filteredGroupMembers = useMemo(
    () =>
      normalizedSearch
        ? groupMembers.filter((person) =>
            person.name.toLowerCase().includes(normalizedSearch)
          )
        : groupMembers,
    [groupMembers, normalizedSearch]
  );

  const addPersonMutation = usePersonGroupsAddPerson({
    mutation: {
      onSuccess: async (_, variables) => {
        await queryClient.invalidateQueries({ queryKey: personGroupsQueryKey });

        const personName = persons.find((person) => person.id === variables.personId)?.name;

        toast({
          title: 'Person added',
          description: personName
            ? `${personName} has been added to the group.`
            : 'Person has been added to the group.',
        });
      },
      onError: () => {
        toast({
          title: 'Failed to add person',
          description: 'Something went wrong while adding the person to the group. Please try again.',
          variant: 'destructive',
        });
      },
    },
  });

  const removePersonMutation = usePersonGroupsRemovePerson({
    mutation: {
      onSuccess: async (_, variables) => {
        await queryClient.invalidateQueries({ queryKey: personGroupsQueryKey });

        const personName = persons.find((person) => person.id === variables.personId)?.name;

        toast({
          title: 'Person removed',
          description: personName
            ? `${personName} has been removed from the group.`
            : 'Person has been removed from the group.',
        });
      },
      onError: () => {
        toast({
          title: 'Failed to remove person',
          description: 'Something went wrong while removing the person from the group. Please try again.',
          variant: 'destructive',
        });
      },
    },
  });

  const handleAddPerson = (person: PersonDto) => {
    if (!isValidGroupId) return;

    addPersonMutation.mutate({ groupId, personId: person.id });
  };

  const handleRemovePerson = (person: PersonDto) => {
    if (!isValidGroupId) return;

    removePersonMutation.mutate({ groupId, personId: person.id });
  };

  const isLoading = isGroupLoading || isPersonsLoading;
  const hasError = isGroupError || isPersonsError;
  const isRefreshing = isGroupFetching || isPersonsFetching;

  if (!isValidGroupId) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center space-y-4">
          <h2 className="text-lg font-medium">Group not found</h2>
          <Button onClick={() => navigate('/admin/person-groups')}>
            <ArrowLeft className="h-4 w-4" />
            Back to Groups
          </Button>
        </div>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center space-y-2">
          <p className="text-sm text-muted-foreground">Loading person group…</p>
        </div>
      </div>
    );
  }

  if (hasError) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center space-y-4">
          <h2 className="text-lg font-medium">Unable to load person group</h2>
          <p className="text-sm text-muted-foreground">
            We couldn&apos;t load the person group details. Please try again.
          </p>
          <div className="flex flex-col sm:flex-row gap-2 justify-center">
            <Button
              variant="outline"
              onClick={() => {
                refetchGroups();
                refetchPersons();
              }}
            >
              Try again
            </Button>
            <Button onClick={() => navigate('/admin/person-groups')}>
              <ArrowLeft className="h-4 w-4" />
              Back to Groups
            </Button>
          </div>
        </div>
      </div>
    );
  }

  if (!group) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center space-y-4">
          <h2 className="text-lg font-medium">Group not found</h2>
          <Button onClick={() => navigate('/admin/person-groups')}>
            <ArrowLeft className="h-4 w-4" />
            Back to Groups
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div className="flex items-center gap-4">
          <Button
            variant="outline"
            size="icon"
            onClick={() => navigate('/admin/person-groups')}
            className="shrink-0"
          >
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div>
            <h1 className="text-2xl font-semibold tracking-tight">{group.name}</h1>
            <p className="text-muted-foreground">
              Manage group members
              {isRefreshing && <span className="ml-2 text-xs">(Refreshing…)</span>}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Badge variant="secondary" className="flex items-center gap-1">
            <Users className="h-3 w-3" />
            {groupMembers.length} members
          </Badge>
        </div>
      </div>

      <div className="flex flex-col lg:flex-row gap-6">
        {/* Search */}
        <div className="lg:hidden">
          <div className="relative">
            <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search persons..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-9"
            />
          </div>
        </div>

        {/* Available Persons */}
        <Card className="flex-1">
          <CardHeader>
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
              <CardTitle className="flex items-center gap-2">
                <UserPlus className="h-5 w-5" />
                Available Persons ({filteredAvailablePersons.length})
              </CardTitle>
              <div className="hidden lg:block relative">
                <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search persons..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-9 w-64"
                />
              </div>
            </div>
          </CardHeader>
          <CardContent className="space-y-2 max-h-96 overflow-y-auto">
            {filteredAvailablePersons.length === 0 ? (
              <div className="text-center py-8 text-muted-foreground">
                {searchTerm ? 'No persons match your search' : 'All persons are already in the group'}
              </div>
            ) : (
              filteredAvailablePersons.map((person) => (
                <div
                  key={person.id}
                  className="flex items-center justify-between p-3 border rounded-lg hover:bg-accent/50 transition-colors"
                >
                  <div className="flex items-center gap-3 flex-1 min-w-0">
                    <Avatar className="h-8 w-8">
                      <AvatarFallback className="text-xs">
                        {person.name.charAt(0).toUpperCase()}
                      </AvatarFallback>
                    </Avatar>
                    <div className="flex-1 min-w-0">
                      <p className="font-medium text-sm truncate">{person.name}</p>
                    </div>
                  </div>
                  <Button
                    size="sm"
                    onClick={() => handleAddPerson(person)}
                    className="shrink-0 ml-2"
                    disabled={addPersonMutation.isPending}
                  >
                    <UserPlus className="h-4 w-4" />
                    <span className="hidden sm:inline ml-1">Add</span>
                  </Button>
                </div>
              ))
            )}
          </CardContent>
        </Card>

        {/* Group Members */}
        <Card className="flex-1">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Users className="h-5 w-5" />
              Group Members ({filteredGroupMembers.length})
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-2 max-h-96 overflow-y-auto">
            {filteredGroupMembers.length === 0 ? (
              <div className="text-center py-8 text-muted-foreground">
                {searchTerm ? 'No members match your search' : 'No members in this group yet'}
              </div>
            ) : (
              filteredGroupMembers.map((person) => (
                <div
                  key={person.id}
                  className="flex items-center justify-between p-3 border rounded-lg hover:bg-accent/50 transition-colors"
                >
                  <div className="flex items-center gap-3 flex-1 min-w-0">
                    <Avatar className="h-8 w-8">
                      <AvatarFallback className="text-xs">
                        {person.name.charAt(0).toUpperCase()}
                      </AvatarFallback>
                    </Avatar>
                    <div className="flex-1 min-w-0">
                      <p className="font-medium text-sm truncate">{person.name}</p>
                    </div>
                  </div>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleRemovePerson(person)}
                    className="shrink-0 ml-2 text-destructive hover:text-destructive"
                    disabled={removePersonMutation.isPending}
                  >
                    <UserMinus className="h-4 w-4" />
                    <span className="hidden sm:inline ml-1">Remove</span>
                  </Button>
                </div>
              ))
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
