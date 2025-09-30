import { useEffect, useMemo, useState } from 'react';
import { Plus, MoreVertical, User, Edit, Trash2, Search, ChevronLeft, ChevronRight } from 'lucide-react';
import type { PersonDto } from '@photobank/shared';
import { useQueryClient } from '@tanstack/react-query';
import {
  getPersonsGetAllQueryKey,
  usePersonsCreate,
  usePersonsDelete,
  usePersonsGetAll,
  usePersonsUpdate,
} from '@photobank/shared/api/photobank';

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

const ITEMS_PER_PAGE = 20;

type IdentityStatus = 'Verified' | 'Pending' | 'Rejected';
type PersonWithIdentityStatus = PersonDto & { identityStatus?: IdentityStatus };

export default function PersonsPage() {
  const { toast } = useToast();
  const queryClient = useQueryClient();
  const personsQueryKey = useMemo(() => getPersonsGetAllQueryKey(), []);
  const { data, isLoading, isError, isFetching, refetch } = usePersonsGetAll();
  const persons = useMemo<PersonWithIdentityStatus[]>(() => {
    const personsData = data?.data ?? [];

    return personsData.map((person) => {
      const maybeWithStatus = person as PersonWithIdentityStatus;

      return {
        ...person,
        identityStatus: maybeWithStatus.identityStatus ?? 'Pending',
      };
    });
  }, [data]);
  const [searchTerm, setSearchTerm] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [showEditDialog, setShowEditDialog] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [selectedPerson, setSelectedPerson] = useState<PersonWithIdentityStatus | null>(null);
  const [newPersonName, setNewPersonName] = useState('');
  const [newPersonIdentityStatus, setNewPersonIdentityStatus] = useState<IdentityStatus>('Pending');

  const hasPersonsLoaded = persons.length > 0;
  const showLoading = isLoading && !hasPersonsLoaded;
  const showError = isError && !hasPersonsLoaded;
  const isRefreshing = isFetching && !showLoading;

  const filteredPersons = useMemo(() => {
    const normalizedSearch = searchTerm.trim().toLowerCase();

    if (!normalizedSearch) {
      return persons;
    }

    return persons.filter((person) =>
      person.name.toLowerCase().includes(normalizedSearch)
    );
  }, [persons, searchTerm]);

  const totalPages = Math.ceil(filteredPersons.length / ITEMS_PER_PAGE);
  const effectiveCurrentPage = totalPages === 0 ? 1 : Math.min(currentPage, totalPages);
  const startIndex = (effectiveCurrentPage - 1) * ITEMS_PER_PAGE;
  const endIndex = startIndex + ITEMS_PER_PAGE;
  const currentPersons = useMemo(
    () => filteredPersons.slice(startIndex, endIndex),
    [filteredPersons, startIndex, endIndex]
  );
  const rangeStart = filteredPersons.length === 0 ? 0 : startIndex + 1;
  const rangeEnd = filteredPersons.length === 0 ? 0 : Math.min(startIndex + currentPersons.length, filteredPersons.length);

  useEffect(() => {
    setCurrentPage(1);
  }, [searchTerm]);

  useEffect(() => {
    if (currentPage !== effectiveCurrentPage) {
      setCurrentPage(effectiveCurrentPage);
    }
  }, [currentPage, effectiveCurrentPage]);

  const createPersonMutation = usePersonsCreate({
    mutation: {
      onSuccess: async (response) => {
        toast({
          title: 'Person created',
          description: `${response.data.name} has been created successfully.`,
        });

        await queryClient.invalidateQueries({ queryKey: personsQueryKey });
      },
      onError: () => {
        toast({
          title: 'Failed to create person',
          description: 'Something went wrong while creating the person. Please try again.',
          variant: 'destructive',
        });
      },
      onSettled: () => {
        setShowCreateDialog(false);
        setNewPersonName('');
        setNewPersonIdentityStatus('Pending');
      },
    },
  });

  const updatePersonMutation = usePersonsUpdate({
    mutation: {
      onSuccess: async (response) => {
        toast({
          title: 'Person updated',
          description: `${response.data.name} has been updated successfully.`,
        });

        await queryClient.invalidateQueries({ queryKey: personsQueryKey });
      },
      onError: () => {
        toast({
          title: 'Failed to update person',
          description: 'Something went wrong while updating the person. Please try again.',
          variant: 'destructive',
        });
      },
      onSettled: () => {
        setShowEditDialog(false);
        setSelectedPerson(null);
        setNewPersonName('');
        setNewPersonIdentityStatus('Pending');
      },
    },
  });

  const deletePersonMutation = usePersonsDelete({
    mutation: {
      onSuccess: async () => {
        if (selectedPerson) {
          toast({
            title: 'Person deleted',
            description: `${selectedPerson.name} has been deleted successfully.`,
          });
        } else {
          toast({
            title: 'Person deleted',
            description: 'Person has been deleted successfully.',
          });
        }

        await queryClient.invalidateQueries({ queryKey: personsQueryKey });
      },
      onError: () => {
        toast({
          title: 'Failed to delete person',
          description: 'Something went wrong while deleting the person. Please try again.',
          variant: 'destructive',
        });
      },
      onSettled: () => {
        setShowDeleteDialog(false);
        setSelectedPerson(null);
      },
    },
  });

  const getStatusColor = (status: IdentityStatus) => {
    switch (status) {
      case 'Verified':
        return 'bg-green-500/10 text-green-700 border-green-200 dark:text-green-400 dark:border-green-800';
      case 'Pending':
        return 'bg-yellow-500/10 text-yellow-700 border-yellow-200 dark:text-yellow-400 dark:border-yellow-800';
      case 'Rejected':
        return 'bg-red-500/10 text-red-700 border-red-200 dark:text-red-400 dark:border-red-800';
      default:
        return 'bg-gray-500/10 text-gray-700 border-gray-200 dark:text-gray-400 dark:border-gray-800';
    }
  };

  const handleCreatePerson = async () => {
    const trimmedName = newPersonName.trim();
    if (!trimmedName) return;

    try {
      await createPersonMutation.mutateAsync({
        data: {
          id: 0,
          name: trimmedName,
        },
      });
    } catch (error) {
      // Error is surfaced via toast handlers.
    }
  };

  const handleEditPerson = (person: PersonWithIdentityStatus) => {
    setSelectedPerson(person);
    setNewPersonName(person.name);
    setNewPersonIdentityStatus(person.identityStatus ?? 'Pending');
    setShowEditDialog(true);
  };

  const handleUpdatePerson = async () => {
    if (!selectedPerson) return;

    const trimmedName = newPersonName.trim();
    if (!trimmedName) return;

    try {
      await updatePersonMutation.mutateAsync({
        personId: selectedPerson.id,
        data: {
          id: selectedPerson.id,
          name: trimmedName,
        },
      });
    } catch (error) {
      // Error is surfaced via toast handlers.
    }
  };

  const handleDeletePerson = (person: PersonWithIdentityStatus) => {
    setSelectedPerson(person);
    setShowDeleteDialog(true);
  };

  const confirmDelete = async () => {
    if (!selectedPerson) return;

    try {
      await deletePersonMutation.mutateAsync({ personId: selectedPerson.id });
    } catch (error) {
      // Error is surfaced via toast handlers.
    }
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
        <div className="flex items-center gap-3">
          {isRefreshing ? (
            <span className="text-xs text-muted-foreground">Refreshing…</span>
          ) : null}
          <Button onClick={() => setShowCreateDialog(true)} size="default" className="w-full sm:w-auto">
            <Plus className="h-4 w-4" />
            Create Person
          </Button>
        </div>
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

      {showLoading ? (
        <Card>
          <CardContent className="flex items-center justify-center py-12">
            <span className="text-sm text-muted-foreground">Loading persons…</span>
          </CardContent>
        </Card>
      ) : showError ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-3 py-12 text-center">
            <p className="text-sm text-destructive">Failed to load persons.</p>
            <Button variant="outline" onClick={() => refetch()}>
              Try again
            </Button>
          </CardContent>
        </Card>
      ) : (
        <>
          {filteredPersons.length > 0 && (
            <>
              {/* Mobile Card Layout */}
              <div className="grid gap-4 md:hidden">
                {currentPersons.map((person) => {
                  const status = person.identityStatus ?? 'Pending';

                  return (
                    <Card key={person.id} className="p-4">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-3">
                          <div className="h-10 w-10 rounded-full bg-muted flex items-center justify-center">
                            <User className="h-5 w-5 text-muted-foreground" />
                          </div>
                          <div>
                            <h3 className="font-medium">{person.name}</h3>
                            <p className="text-sm text-muted-foreground">ID: {person.id}</p>
                            <Badge variant="outline" className={getStatusColor(status)}>
                              {status}
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
                  );
                })}
              </div>

              {/* Desktop Table Layout */}
              <div className="hidden md:block">
                <Card>
                  <CardHeader>
                    <CardTitle>All Persons</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-4">
                      {currentPersons.map((person) => {
                        const status = person.identityStatus ?? 'Pending';

                        return (
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
                              <Badge variant="outline" className={getStatusColor(status)}>
                                {status}
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
                        );
                      })}
                    </div>
                  </CardContent>
                </Card>
              </div>
            </>
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

          {filteredPersons.length > 0 && totalPages > 1 && (
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">
                Showing {rangeStart}-{rangeEnd} of {filteredPersons.length} persons
              </p>
              <Pagination>
                <PaginationContent>
                  <PaginationItem>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setCurrentPage((prev) => Math.max(prev - 1, 1))}
                      disabled={effectiveCurrentPage === 1}
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
                    } else if (effectiveCurrentPage <= 3) {
                      pageNumber = i + 1;
                    } else if (effectiveCurrentPage >= totalPages - 2) {
                      pageNumber = totalPages - 4 + i;
                    } else {
                      pageNumber = effectiveCurrentPage - 2 + i;
                    }

                    return (
                      <PaginationItem key={pageNumber}>
                        <PaginationLink
                          onClick={() => setCurrentPage(pageNumber)}
                          isActive={effectiveCurrentPage === pageNumber}
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
                      onClick={() => setCurrentPage((prev) => Math.min(prev + 1, totalPages))}
                      disabled={effectiveCurrentPage === totalPages}
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
        </>
      )}

      {/* Create Person Dialog */}
      <Dialog
        open={showCreateDialog}
        onOpenChange={(open) => {
          if (!createPersonMutation.isPending) {
            setShowCreateDialog(open);
          }
        }}
      >
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
              <Select value={newPersonIdentityStatus} onValueChange={(value: IdentityStatus) => setNewPersonIdentityStatus(value)}>
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
            <Button
              variant="outline"
              onClick={() => {
                if (!createPersonMutation.isPending) {
                  setShowCreateDialog(false);
                }
              }}
              disabled={createPersonMutation.isPending}
            >
              Cancel
            </Button>
            <Button onClick={handleCreatePerson} disabled={!newPersonName.trim() || createPersonMutation.isPending}>
              Create Person
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Person Dialog */}
      <Dialog
        open={showEditDialog}
        onOpenChange={(open) => {
          if (!updatePersonMutation.isPending) {
            setShowEditDialog(open);
          }
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit Person</DialogTitle>
            <DialogDescription>
              Update the person's information.
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
              <Select value={newPersonIdentityStatus} onValueChange={(value: IdentityStatus) => setNewPersonIdentityStatus(value)}>
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
            <Button
              variant="outline"
              onClick={() => {
                if (!updatePersonMutation.isPending) {
                  setShowEditDialog(false);
                  setSelectedPerson(null);
                }
              }}
              disabled={updatePersonMutation.isPending}
            >
              Cancel
            </Button>
            <Button onClick={handleUpdatePerson} disabled={!newPersonName.trim() || updatePersonMutation.isPending}>
              Update Person
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <AlertDialog
        open={showDeleteDialog}
        onOpenChange={(open) => {
          if (!deletePersonMutation.isPending) {
            setShowDeleteDialog(open);
          }
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Person</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete "{selectedPerson?.name}"? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={confirmDelete}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              disabled={deletePersonMutation.isPending}
            >
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
