import { useEffect, useMemo, useState } from 'react';
import { Search, User, Calendar, Eye, Loader2 } from 'lucide-react';
import type { FaceIdentityDto } from '@photobank/shared/api/photobank';
import { useFacesGet } from '@photobank/shared/api/photobank';

import { Button } from '@/shared/ui/button';
import { Input } from '@/shared/ui/input';
import { Badge } from '@/shared/ui/badge';
import { Card, CardContent } from '@/shared/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/ui/table';
import { EditFaceDialog, type EditFaceDialogFace } from '@/components/admin/EditFaceDialog';
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

type FaceListItem = EditFaceDialogFace &
  Partial<{
    createdAt: string | Date | null;
    updatedAt: string | Date | null;
  }>;

export default function FacesPage() {
  const [searchTerm, setSearchTerm] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [selectedFace, setSelectedFace] = useState<FaceListItem | null>(null);

  const { data, isLoading, isError, isFetching, refetch } = useFacesGet();

  const faces = useMemo<FaceListItem[]>(() => {
    const rawFaces = (data?.data ?? []) as FaceIdentityDto[];

    return rawFaces.map((face) => {
      const extendedFace = face as FaceListItem;
      const normalizedPersonId =
        extendedFace.personId ?? (extendedFace.person ? extendedFace.person.id ?? null : null);
      const normalizedPersonName =
        extendedFace.personName ?? (extendedFace.person ? extendedFace.person.name ?? null : null);
      const normalizedFaceId = extendedFace.faceId ?? extendedFace.id ?? null;

      return {
        ...extendedFace,
        faceId: normalizedFaceId,
        id: extendedFace.id ?? normalizedFaceId ?? undefined,
        personId: normalizedPersonId,
        personName: normalizedPersonName,
      };
    });
  }, [data]);

  const hasFacesLoaded = faces.length > 0;
  const showLoading = isLoading && !hasFacesLoaded;
  const showError = isError && !hasFacesLoaded;
  const isRefreshing = isFetching && !showLoading;

  const normalizedSearch = searchTerm.trim().toLowerCase();
  const filteredFaces = useMemo(() => {
    if (!normalizedSearch) {
      return faces;
    }

    return faces.filter((face) => {
      const id = (face.id ?? face.faceId)?.toString() ?? '';
      const faceId = face.faceId?.toString() ?? '';
      const rawPersonId =
        face.personId ?? (face.person ? face.person.id ?? null : null);
      const personId = rawPersonId != null ? rawPersonId.toString() : '';
      const rawPersonName = face.personName ?? (face.person ? face.person.name ?? null : null);
      const personName = rawPersonName?.toLowerCase() ?? '';
      const identityStatus = face.identityStatus?.toLowerCase() ?? '';
      const provider = face.provider?.toLowerCase() ?? '';
      const externalId = face.externalId?.toLowerCase() ?? '';

      return (
        id.includes(normalizedSearch) ||
        faceId.includes(normalizedSearch) ||
        personId.includes(normalizedSearch) ||
        personName.includes(normalizedSearch) ||
        identityStatus.includes(normalizedSearch) ||
        provider.includes(normalizedSearch) ||
        externalId.includes(normalizedSearch)
      );
    });
  }, [faces, normalizedSearch]);

  const totalPages = Math.ceil(filteredFaces.length / ITEMS_PER_PAGE);
  const effectiveCurrentPage = totalPages === 0 ? 1 : Math.min(currentPage, totalPages);
  const startIndex = (effectiveCurrentPage - 1) * ITEMS_PER_PAGE;
  const endIndex = startIndex + ITEMS_PER_PAGE;

  const currentFaces = useMemo(
    () => filteredFaces.slice(startIndex, endIndex),
    [filteredFaces, startIndex, endIndex]
  );

  useEffect(() => {
    if (currentPage !== effectiveCurrentPage) {
      setCurrentPage(effectiveCurrentPage);
    }
  }, [currentPage, effectiveCurrentPage]);

  const handleEditFace = (face: FaceListItem) => {
    setSelectedFace(face);
    setEditDialogOpen(true);
  };

  const handleDialogOpenChange = (open: boolean) => {
    setEditDialogOpen(open);
    if (!open) {
      setSelectedFace(null);
    }
  };

  const getStatusColor = (status?: string | null) => {
    switch (status) {
      case 'Verified':
      case 'Identified':
        return 'bg-success text-success-foreground';
      case 'Pending':
      case 'NotDetected':
      case 'NotIdentified':
      case 'ForReprocessing':
        return 'bg-warning text-warning-foreground';
      case 'Rejected':
      case 'StopProcessing':
        return 'bg-destructive text-destructive-foreground';
      default:
        return 'bg-muted text-muted-foreground';
    }
  };

  const formatDate = (dateInput?: string | Date | null) => {
    if (!dateInput) {
      return null;
    }

    const date = typeof dateInput === 'string' ? new Date(dateInput) : new Date(dateInput);

    if (Number.isNaN(date.getTime())) {
      return null;
    }

    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const statusLabel = (status?: string | null) => {
    if (!status) {
      return 'Unknown';
    }

    return status;
  };

  const displayedFacesCount = showLoading ? '—' : filteredFaces.length;
  const totalFacesCount = showLoading ? '—' : faces.length;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-foreground">Faces</h1>
          <p className="text-muted-foreground">
            Manage face recognition data and identity verification
          </p>
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
                onChange={(e) => {
                  setSearchTerm(e.target.value);
                  setCurrentPage(1);
                }}
                className="pl-10"
                data-testid="faces-search-input"
              />
            </div>
            <div className="text-sm text-muted-foreground flex items-center gap-2">
              {isRefreshing && <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />}
              <span>
                {displayedFacesCount} of {totalFacesCount} faces
              </span>
            </div>
          </div>

          {showError ? (
            <div className="flex flex-col items-center justify-center gap-4 py-12 text-center">
              <p className="text-sm text-muted-foreground">
                We couldn&apos;t load the faces list. Please try again.
              </p>
              <Button variant="outline" onClick={() => refetch()}>
                Retry loading faces
              </Button>
            </div>
          ) : showLoading ? (
            <div className="flex items-center justify-center py-12" role="status">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
              <span className="sr-only">Loading faces…</span>
            </div>
          ) : currentFaces.length > 0 ? (
            <>
              <div className="rounded-lg border border-border overflow-hidden">
                <Table>
                  <TableHeader>
                    <TableRow className="bg-muted/50">
                      <TableHead className="w-12">ID</TableHead>
                      <TableHead>Face</TableHead>
                      <TableHead>Person</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Created</TableHead>
                      <TableHead>Updated</TableHead>
                      <TableHead className="text-right">Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {currentFaces.map((face) => {
                      const createdAt = formatDate(face.createdAt);
                      const updatedAt = formatDate(face.updatedAt);
                      const personId =
                        face.personId ?? (face.person ? face.person.id ?? null : null);
                      const displayName =
                        face.personName ??
                        (face.person ? face.person.name ?? null : null) ??
                        (personId != null ? `Person #${personId}` : null);
                      const status = statusLabel(face.identityStatus);
                      const faceIdentifier = face.id ?? face.faceId;

                      return (
                        <TableRow key={faceIdentifier ?? face.faceId} className="hover:bg-muted/30">
                          <TableCell className="font-medium">#{faceIdentifier ?? face.faceId}</TableCell>
                          <TableCell>
                            <Avatar className="w-10 h-10">
                              <AvatarImage src={face.imageUrl ?? undefined} />
                              <AvatarFallback>
                                <User className="w-4 h-4" />
                              </AvatarFallback>
                            </Avatar>
                          </TableCell>
                          <TableCell>
                            {displayName ? (
                              <div className="flex items-center gap-2">
                                <User className="w-4 h-4 text-muted-foreground" />
                                <span>{displayName}</span>
                              </div>
                            ) : (
                              <span className="text-muted-foreground italic">Unassigned</span>
                            )}
                          </TableCell>
                          <TableCell>
                            <Badge className={getStatusColor(face.identityStatus)}>{status}</Badge>
                          </TableCell>
                          <TableCell>
                            {createdAt ? (
                              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                <Calendar className="w-4 h-4" />
                                {createdAt}
                              </div>
                            ) : (
                              <span className="text-muted-foreground">—</span>
                            )}
                          </TableCell>
                          <TableCell>
                            {updatedAt ? (
                              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                <Calendar className="w-4 h-4" />
                                {updatedAt}
                              </div>
                            ) : (
                              <span className="text-muted-foreground">—</span>
                            )}
                          </TableCell>
                          <TableCell className="text-right">
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => handleEditFace(face)}
                              className="gap-2"
                            >
                              <Eye className="w-4 h-4" />
                              Edit
                            </Button>
                          </TableCell>
                        </TableRow>
                      );
                    })}
                  </TableBody>
                </Table>
              </div>

              {totalPages > 1 && (
                <div className="flex justify-center mt-6">
                  <Pagination>
                    <PaginationContent>
                      <PaginationItem>
                        <PaginationPrevious
                          onClick={() =>
                            setCurrentPage((prev) => Math.max(1, prev - 1))
                          }
                          className={
                            effectiveCurrentPage === 1
                              ? 'pointer-events-none opacity-50'
                              : 'cursor-pointer'
                          }
                        />
                      </PaginationItem>

                      {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                        let pageNum: number;
                        if (totalPages <= 5) {
                          pageNum = i + 1;
                        } else if (effectiveCurrentPage <= 3) {
                          pageNum = i + 1;
                        } else if (effectiveCurrentPage >= totalPages - 2) {
                          pageNum = totalPages - 4 + i;
                        } else {
                          pageNum = effectiveCurrentPage - 2 + i;
                        }

                        return (
                          <PaginationItem key={pageNum}>
                            <PaginationLink
                              onClick={() => setCurrentPage(pageNum)}
                              isActive={pageNum === effectiveCurrentPage}
                              className="cursor-pointer"
                            >
                              {pageNum}
                            </PaginationLink>
                          </PaginationItem>
                        );
                      })}

                      <PaginationItem>
                        <PaginationNext
                          onClick={() =>
                            setCurrentPage((prev) => Math.min(totalPages, prev + 1))
                          }
                          className={
                            effectiveCurrentPage === totalPages
                              ? 'pointer-events-none opacity-50'
                              : 'cursor-pointer'
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
              {normalizedSearch
                ? 'No faces match your search criteria.'
                : 'No faces available yet.'}
            </div>
          )}
        </CardContent>
      </Card>

      <EditFaceDialog
        open={editDialogOpen}
        onOpenChange={handleDialogOpenChange}
        face={selectedFace}
      />
    </div>
  );
}
