import { useState } from 'react';
import { Plus, MoreVertical, User, Edit, Trash2, Search, ChevronLeft, ChevronRight } from 'lucide-react';
import type { PersonDto } from '@photobank/shared';

import { Button } from '@/shared/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { Input } from '@/shared/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/shared/ui/select';
import { Badge } from '@/shared/ui/badge';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/shared/ui/dialog';
import { Label } from '@/shared/ui/label';
import { useToast } from '@/hooks/use-toast';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/shared/ui/dropdown-menu';
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from '@/shared/ui/alert-dialog';
import { Pagination, PaginationContent, PaginationItem, PaginationLink } from '@/shared/ui/pagination';

import { mockPersons } from '@/data/mockData';
import { Person } from '@/types/admin';

const ITEMS_PER_PAGE = 20;

export default function PersonsPage() {
  const { toast } = useToast();
  const [persons, setPersons] = useState<PersonDto[]>(mockPersons);
  const [searchTerm, setSearchTerm] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [showEditDialog, setShowEditDialog] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [selectedPerson, setSelectedPerson] = useState<PersonDto | null>(null);
  const [newPersonName, setNewPersonName] = useState('');
  const [newPersonIdentityStatus, setNewPersonIdentityStatus] = useState<PersonDto['identityStatus']>('Pending');

  const filteredPersons = persons.filter(person =>
    person.name.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const totalPages = Math.ceil(filteredPersons.length / ITEMS_PER_PAGE);
  const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
  const endIndex = startIndex + ITEMS_PER_PAGE;
  const currentPersons = filteredPersons.slice(startIndex, endIndex);

  const getStatusColor = (status: PersonDto['identityStatus']) => {
    switch (status) {
      case 'Verified': return 'bg-green-500/10 text-green-700 border-green-200 dark:text-green-400 dark:border-green-800';
      case 'Pending': return 'bg-yellow-500/10 text-yellow-700 border-yellow-200 dark:text-yellow-400 dark:border-yellow-800';
      case 'Rejected': return 'bg-red-500/10 text-red-700 border-red-200 dark:text-red-400 dark:border-red-800';
      default: return 'bg-gray-500/10 text-gray-700 border-gray-200 dark:text-gray-400 dark:border-gray-800';
    }
  };

  const handleCreatePerson = () => {
    if (!newPersonName.trim()) return;
    
    const newPerson: Person = {
      id: Math.max(...persons.map(p => p.id), 0) + 1,
      name: newPersonName.trim(),
      identityStatus: newPersonIdentityStatus
    };
    
    setPersons([...persons, newPerson]);
    setShowCreateDialog(false);
    setNewPersonName('');
    setNewPersonIdentityStatus('Pending');
    toast({
      title: "Person created",
      description: `${newPerson.name} has been created successfully.`,
    });
  };

  const handleEditPerson = (person: Person) => {
    setSelectedPerson(person);
    setNewPersonName(person.name);
    setNewPersonIdentityStatus(person.identityStatus);
    setShowEditDialog(true);
  };

  const handleUpdatePerson = () => {
    if (!selectedPerson || !newPersonName.trim()) return;
    
    setPersons(persons.map(p => 
      p.id === selectedPerson.id 
        ? { ...p, name: newPersonName.trim(), identityStatus: newPersonIdentityStatus }
        : p
    ));
    setShowEditDialog(false);
    setSelectedPerson(null);
    setNewPersonName('');
    setNewPersonIdentityStatus('Pending');
    toast({
      title: "Person updated",
      description: `Person has been updated successfully.`,
    });
  };

  const handleDeletePerson = (person: Person) => {
    setSelectedPerson(person);
    setShowDeleteDialog(true);
  };

  const confirmDelete = () => {
    if (selectedPerson) {
      setPersons(persons.filter(p => p.id !== selectedPerson.id));
      toast({
        title: "Person deleted",
        description: `${selectedPerson.name} has been deleted successfully.`,
      });
    }
    setShowDeleteDialog(false);
    setSelectedPerson(null);
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Persons</h1>
          <p className="text-muted-foreground">
            Manage individual persons in the system
          </p>
        </div>
        <Button onClick={() => setShowCreateDialog(true)} size="default" className="w-full sm:w-auto">
          <Plus className="h-4 w-4" />
          Create Person
        </Button>
      </div>

      {/* Search */}
      <div className="relative max-w-sm">
        <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
        <Input
          placeholder="Search persons..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="pl-9"
        />
      </div>

      {/* Mobile Card Layout */}
      <div className="grid gap-4 md:hidden">
        {currentPersons.map((person) => (
          <Card key={person.id} className="p-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="h-10 w-10 rounded-full bg-muted flex items-center justify-center">
                  <User className="h-5 w-5 text-muted-foreground" />
                </div>
                <div>
                  <h3 className="font-medium">{person.name}</h3>
                  <p className="text-sm text-muted-foreground">ID: {person.id}</p>
                  <Badge variant="outline" className={getStatusColor(person.identityStatus)}>
                    {person.identityStatus}
                  </Badge>
                </div>
              </div>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="icon" className="h-8 w-8">
                    <MoreVertical className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem onClick={() => handleEditPerson(person)}>
                    <Edit className="h-4 w-4" />
                    Edit Person
                  </DropdownMenuItem>
                  <DropdownMenuItem 
                    onClick={() => handleDeletePerson(person)}
                    className="text-destructive"
                  >
                    <Trash2 className="h-4 w-4" />
                    Delete
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </Card>
        ))}
      </div>

      {/* Desktop Table Layout */}
      <div className="hidden md:block">
        <Card>
          <CardHeader>
            <CardTitle>All Persons</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {currentPersons.map((person) => (
                <div key={person.id} className="flex items-center justify-between p-4 border rounded-lg">
                  <div className="flex items-center gap-3">
                    <div className="h-10 w-10 rounded-full bg-muted flex items-center justify-center">
                      <User className="h-5 w-5 text-muted-foreground" />
                    </div>
                    <div>
                      <h3 className="font-medium">{person.name}</h3>
                      <p className="text-sm text-muted-foreground">ID: {person.id}</p>
                    </div>
                  </div>
                  
                  <div className="flex items-center gap-3">
                    <Badge variant="outline" className={getStatusColor(person.identityStatus)}>
                      {person.identityStatus}
                    </Badge>
                    <div className="flex items-center gap-2">
                      <Button 
                        variant="outline" 
                        size="sm"
                        onClick={() => handleEditPerson(person)}
                      >
                        <Edit className="h-4 w-4" />
                        Edit
                      </Button>
                      <Button 
                        variant="outline" 
                        size="sm"
                        onClick={() => handleDeletePerson(person)}
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

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            Showing {startIndex + 1}-{Math.min(endIndex, filteredPersons.length)} of {filteredPersons.length} persons
          </p>
          <Pagination>
            <PaginationContent>
              <PaginationItem>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setCurrentPage(prev => Math.max(prev - 1, 1))}
                  disabled={currentPage === 1}
                  className="gap-1 pl-2.5"
                >
                  <ChevronLeft className="h-4 w-4" />
                  Previous
                </Button>
              </PaginationItem>
              
              {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                let pageNumber;
                if (totalPages <= 5) {
                  pageNumber = i + 1;
                } else if (currentPage <= 3) {
                  pageNumber = i + 1;
                } else if (currentPage >= totalPages - 2) {
                  pageNumber = totalPages - 4 + i;
                } else {
                  pageNumber = currentPage - 2 + i;
                }
                
                return (
                  <PaginationItem key={pageNumber}>
                    <PaginationLink
                      onClick={() => setCurrentPage(pageNumber)}
                      isActive={currentPage === pageNumber}
                      className="cursor-pointer"
                    >
                      {pageNumber}
                    </PaginationLink>
                  </PaginationItem>
                );
              })}
              
              <PaginationItem>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setCurrentPage(prev => Math.min(prev + 1, totalPages))}
                  disabled={currentPage === totalPages}
                  className="gap-1 pr-2.5"
                >
                  Next
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </PaginationItem>
            </PaginationContent>
          </Pagination>
        </div>
      )}

      {filteredPersons.length === 0 && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <User className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-medium mb-2">No persons found</h3>
            <p className="text-muted-foreground text-center mb-4">
              {searchTerm ? 'No persons match your search criteria' : 'Get started by creating your first person'}
            </p>
            <Button onClick={() => setShowCreateDialog(true)}>
              <Plus className="h-4 w-4" />
              Create Person
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Create Person Dialog */}
      <Dialog open={showCreateDialog} onOpenChange={setShowCreateDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create New Person</DialogTitle>
            <DialogDescription>
              Add a new person to the system.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label htmlFor="name">Name</Label>
              <Input
                id="name"
                placeholder="Enter person name"
                value={newPersonName}
                onChange={(e) => setNewPersonName(e.target.value)}
              />
            </div>
            <div>
              <Label htmlFor="identity-status">Identity Status</Label>
              <Select value={newPersonIdentityStatus} onValueChange={(value: Person['identityStatus']) => setNewPersonIdentityStatus(value)}>
                <SelectTrigger>
                  <SelectValue placeholder="Select identity status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Pending">Pending</SelectItem>
                  <SelectItem value="Verified">Verified</SelectItem>
                  <SelectItem value="Rejected">Rejected</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowCreateDialog(false)}>
              Cancel
            </Button>
            <Button onClick={handleCreatePerson} disabled={!newPersonName.trim()}>
              Create Person
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Person Dialog */}
      <Dialog open={showEditDialog} onOpenChange={setShowEditDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit Person</DialogTitle>
            <DialogDescription>
              Update the person&#39;s information.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label htmlFor="edit-name">Name</Label>
              <Input
                id="edit-name"
                placeholder="Enter person name"
                value={newPersonName}
                onChange={(e) => setNewPersonName(e.target.value)}
              />
            </div>
            <div>
              <Label htmlFor="edit-identity-status">Identity Status</Label>
              <Select value={newPersonIdentityStatus} onValueChange={(value: Person['identityStatus']) => setNewPersonIdentityStatus(value)}>
                <SelectTrigger>
                  <SelectValue placeholder="Select identity status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Pending">Pending</SelectItem>
                  <SelectItem value="Verified">Verified</SelectItem>
                  <SelectItem value="Rejected">Rejected</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowEditDialog(false)}>
              Cancel
            </Button>
            <Button onClick={handleUpdatePerson} disabled={!newPersonName.trim()}>
              Update Person
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Person</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete &quot;{selectedPerson?.name}&quot;? This action cannot be undone.
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