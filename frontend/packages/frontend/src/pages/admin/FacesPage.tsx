import { useEffect, useMemo, useState } from 'react';
import { Search, User, Eye, Loader2 } from 'lucide-react';
import {
  IdentityStatusDto as IdentityStatusEnum,
  useFacesGetAll,
  usePersonsGetAll,
  type FaceDto,
  type IdentityStatusDto as IdentityStatusType,
} from '@photobank/shared/api/photobank';
import { buildPersonMap } from '@photobank/shared/metadata';

import { Button } from '@/shared/ui/button';
import { Input } from '@/shared/ui/input';
import { Badge } from '@/shared/ui/badge';
import { Card, CardContent } from '@/shared/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/ui/table';
import { EditFaceDialog } from '@/components/admin/EditFaceDialog';
import { Avatar, AvatarFallback, AvatarImage } from '@/shared/ui/avatar';
import {
  Pagination,
  PaginationContent,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from '@/shared/ui/pagination';

const ITEMS_PER_PAGE = 20;

const identityStatusValues = Object.values(IdentityStatusEnum) as IdentityStatusType[];

const normalizeIdentityStatus = (value: unknown): IdentityStatusType => {
  if (typeof value === 'string' && value.trim()) {
    const match = identityStatusValues.find(
      (status) => status.toLowerCase() === value.trim().toLowerCase()
    );

    if (match) {
      return match;
    }
  }

  return IdentityStatusEnum.Undefined;
};

const getStatusColor = (status: IdentityStatusType) => {
  switch (status) {
    case IdentityStatusEnum.Identified:
      return 'bg-success text-success-foreground';
    case IdentityStatusEnum.ForReprocessing:
    case IdentityStatusEnum.NotDetected:
    case IdentityStatusEnum.NotIdentified:
      return 'bg-warning text-warning-foreground';
    case IdentityStatusEnum.StopProcessing:
      return 'bg-destructive text-destructive-foreground';
    default:
      return 'bg-muted text-muted-foreground';
  }
};

const formatStatusLabel = (status: IdentityStatusType) =>
  status.replace(/([a-z0-9])([A-Z])/g, '$1 $2');

type FaceRow = {
  face: FaceDto;
  id: number;
  status: IdentityStatusType;
  personName: string | null;
};

export default function FacesPage() {
  const [searchTerm, setSearchTerm] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [selectedFace, setSelectedFace] = useState<FaceDto | null>(null);

  const paginationParams = useMemo(
    () => ({
      page: currentPage,
      pageSize: ITEMS_PER_PAGE,
    }),
    [currentPage]
  );

  const { data, isLoading, isError, isFetching, refetch } = useFacesGetAll(paginationParams);
  const { data: personsResponse } = usePersonsGetAll();
  const persons = personsResponse?.data;

  const personLookup = useMemo(
    () => buildPersonMap(persons ?? null),
    [persons]
  );

  const facesResponse = data?.data;
  const facesItems = facesResponse?.items;
  const rawFaces = useMemo<FaceDto[]>(
    () => (Array.isArray(facesItems) ? (facesItems) : []),
    [facesItems]
  );
  const totalFacesCount = facesResponse?.totalCount ?? 0;

  const faceRows = useMemo<FaceRow[]>(() => {
    return rawFaces.map((face) => {
      const id = face.id ?? 0;
      const personId = face.personId ?? null;
      const status = normalizeIdentityStatus(face.identityStatus);
      const normalizedFace: FaceDto = {
        ...face,
        id,
        personId,
        identityStatus: status,
      };

      return {
        face: normalizedFace,
        id,
        status,
        personName:
          personId != null ? personLookup.get(personId)?.name ?? null : null,
      };
    });
  }, [personLookup, rawFaces]);

  const filteredFaces = useMemo(() => {
    const query = searchTerm.trim().toLowerCase();

    if (!query) {
      return faceRows;
    }

    return faceRows.filter(({ id, personName, status }) => {
      const haystack = [String(id), personName ?? '', formatStatusLabel(status)]
        .join(' ')
        .toLowerCase();

      return haystack.includes(query);
    });
  }, [faceRows, searchTerm]);

  const totalPages = Math.max(1, Math.ceil(totalFacesCount / ITEMS_PER_PAGE));

  useEffect(() => {
    if (!isLoading && currentPage > totalPages) {
      setCurrentPage(totalPages);
    }
  }, [currentPage, totalPages, isLoading]);

  const handleEditFace = (face: FaceDto) => {
    setSelectedFace(face);
    setEditDialogOpen(true);
  };

  const handleDialogOpenChange = (open: boolean) => {
    setEditDialogOpen(open);
    if (!open) {
      setSelectedFace(null);
    }
  };

  const showLoading = isLoading && rawFaces.length === 0;
  const showError = isError && rawFaces.length === 0;
  const isRefreshing = isFetching && !showLoading;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-foreground">Faces</h1>
          <p className="text-muted-foreground">Manage face recognition data and identity verification</p>
        </div>
      </div>
      <Card className="shadow-card">
        <CardContent className="p-6">
          <div className="flex items-center gap-4 mb-6">
            <div className="relative flex-1 max-w-sm">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground w-4 h-4" />
              <Input
                placeholder="Search faces..."
                value={searchTerm}
                onChange={(event) => {
                  setSearchTerm(event.target.value);
                  setCurrentPage(1);
                }}
                className="pl-10"
                data-testid="faces-search-input"
              />
            </div>
            <div className="text-sm text-muted-foreground flex items-center gap-2">
              {isRefreshing && <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />}
              <span>
                {showLoading ? '-' : filteredFaces.length} of {showLoading ? '-' : totalFacesCount} faces
              </span>
            </div>
          </div>
          {showError ? (
            <div className="flex flex-col items-center justify-center gap-4 py-12 text-center">
              <p className="text-sm text-muted-foreground">We couldn't load the faces list. Please try again.</p>
              <Button
                variant="outline"
                onClick={() => {
                  void refetch();
                }}
              >
                Retry loading faces
              </Button>
            </div>
          ) : showLoading ? (
            <div className="flex items-center justify-center py-12" role="status">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
              <span className="sr-only">Loading faces</span>
            </div>
          ) : filteredFaces.length > 0 ? (
            <>
              <div className="rounded-lg border border-border overflow-hidden">
                <Table>
                  <TableHeader>
                    <TableRow className="bg-muted/50">
                      <TableHead className="w-12">ID</TableHead>
                      <TableHead>Face</TableHead>
                      <TableHead>Person</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead className="text-right">Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {filteredFaces.map(({ face, id, status, personName }) => (
                      <TableRow key={id} className="hover:bg-muted/30">
                        <TableCell className="font-medium">#{id}</TableCell>
                        <TableCell>
                          <Avatar className="w-10 h-10">
                            <AvatarImage src={face.imageUrl ?? undefined} />
                            <AvatarFallback>
                              <User className="w-4 h-4" />
                            </AvatarFallback>
                          </Avatar>
                        </TableCell>
                        <TableCell>
                          {personName ? (
                            <div className="flex items-center gap-2">
                              <User className="w-4 h-4 text-muted-foreground" />
                              <span>{personName}</span>
                            </div>
                          ) : (
                            <span className="text-muted-foreground italic">Unassigned</span>
                          )}
                        </TableCell>
                        <TableCell>
                          <Badge className={getStatusColor(status)}>{formatStatusLabel(status)}</Badge>
                        </TableCell>
                        <TableCell className="text-right">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => handleEditFace(face)}
                            className="gap-2"
                          >
                            <Eye className="w-4 h-4" /> Edit
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
              {totalPages > 1 && (
                <div className="flex justify-center mt-6">
                  <Pagination>
                    <PaginationContent>
                      <PaginationItem>
                        <PaginationPrevious
                          onClick={() => setCurrentPage((previous) => Math.max(1, previous - 1))}
                          className={currentPage === 1 ? 'pointer-events-none opacity-50' : 'cursor-pointer'}
                        />
                      </PaginationItem>
                      {Array.from({ length: Math.min(5, totalPages) }, (_, index) => {
                        const pageNumber =
                          totalPages <= 5
                            ? index + 1
                            : currentPage <= 3
                              ? index + 1
                              : currentPage >= totalPages - 2
                                ? totalPages - 4 + index
                                : currentPage - 2 + index;

                        return (
                          <PaginationItem key={pageNumber}>
                            <PaginationLink
                              onClick={() => setCurrentPage(pageNumber)}
                              isActive={pageNumber === currentPage}
                              className="cursor-pointer"
                            >
                              {pageNumber}
                            </PaginationLink>
                          </PaginationItem>
                        );
                      })}
                      <PaginationItem>
                        <PaginationNext
                          onClick={() => setCurrentPage((previous) => Math.min(totalPages, previous + 1))}
                          className={
                            currentPage === totalPages ? 'pointer-events-none opacity-50' : 'cursor-pointer'
                          }
                        />
                      </PaginationItem>
                    </PaginationContent>
                  </Pagination>
                </div>
              )}
            </>
          ) : (
            <div className="py-12 text-center text-muted-foreground">
              {searchTerm.trim() ? 'No faces match your search criteria.' : 'No faces available yet.'}
            </div>
          )}
        </CardContent>
      </Card>
      {selectedFace && (
        <EditFaceDialog open={editDialogOpen} onOpenChange={handleDialogOpenChange} face={selectedFace} />
      )}
    </div>
  );
}