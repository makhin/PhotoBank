import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Search, UserPlus, UserMinus, Users } from 'lucide-react';

import { Button } from '@/shared/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { Badge } from '@/shared/ui/badge';
import { Input } from '@/shared/ui/input';

import { Avatar, AvatarFallback } from '@/shared/ui/avatar';
import { mockPersonGroupsWithMembers, mockPersons } from '@/data/mockData';
import { Person, PersonGroupWithMembers } from '@/types/admin';
import { useToast } from '@/hooks/use-toast';

export default function EditPersonGroupPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { toast } = useToast();
  
  const [group, setGroup] = useState<PersonGroupWithMembers | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [availablePersons, setAvailablePersons] = useState<Person[]>([]);
  const [groupMembers, setGroupMembers] = useState<Person[]>([]);

  useEffect(() => {
    const foundGroup = mockPersonGroupsWithMembers.find(g => g.id === Number(id));
    if (foundGroup) {
      setGroup(foundGroup);
      setGroupMembers(foundGroup.members);
      setAvailablePersons(mockPersons.filter(person => 
        !foundGroup.members.some(member => member.id === person.id)
      ));
    }
  }, [id]);

  const filteredAvailablePersons = availablePersons.filter(person =>
    person.name.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const filteredGroupMembers = groupMembers.filter(person =>
    person.name.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const addPersonToGroup = (person: Person) => {
    setAvailablePersons(availablePersons.filter(p => p.id !== person.id));
    setGroupMembers([...groupMembers, person]);
    toast({
      title: "Person added",
      description: `${person.name} has been added to the group.`,
    });
  };

  const removePersonFromGroup = (person: Person) => {
    setGroupMembers(groupMembers.filter(p => p.id !== person.id));
    setAvailablePersons([...availablePersons, person]);
    toast({
      title: "Person removed",
      description: `${person.name} has been removed from the group.`,
    });
  };

  if (!group) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <h2 className="text-lg font-medium mb-2">Group not found</h2>
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
            <p className="text-muted-foreground">Manage group members</p>
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
                    onClick={() => addPersonToGroup(person)}
                    className="shrink-0 ml-2"
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
                    onClick={() => removePersonFromGroup(person)}
                    className="shrink-0 ml-2 text-destructive hover:text-destructive"
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