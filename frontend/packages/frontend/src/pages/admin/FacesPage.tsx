import { useState } from 'react';
import { Search, User, Calendar, Eye } from 'lucide-react';

import { Button } from '@/shared/ui/button';
import { Input } from '@/shared/ui/input';
import { Badge } from '@/shared/ui/badge';
import { Card, CardContent } from '@/shared/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/ui/table';
import { EditFaceDialog } from '@/components/admin/EditFaceDialog';

import { Avatar, AvatarFallback, AvatarImage } from '@/shared/ui/avatar';
import { Pagination, PaginationContent, PaginationItem, PaginationLink, PaginationNext, PaginationPrevious } from '@/shared/ui/pagination';
import { Face } from '@/types/admin';
import { mockFaces } from '@/data/mockData';
import { useToast } from '@/hooks/use-toast';

const ITEMS_PER_PAGE = 20;

export default function FacesPage() {
  const [faces, setFaces] = useState<Face[]>(mockFaces);
  const [searchTerm, setSearchTerm] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [selectedFace, setSelectedFace] = useState<Face | null>(null);
  const { toast } = useToast();

  const filteredFaces = faces.filter(face =>
    face.personName?.toLowerCase().includes(searchTerm.toLowerCase()) ||
    face.id.toString().includes(searchTerm) ||
    face.identityStatus.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const totalPages = Math.ceil(filteredFaces.length / ITEMS_PER_PAGE);
  const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
  const currentFaces = filteredFaces.slice(startIndex, startIndex + ITEMS_PER_PAGE);

  const getStatusColor = (status: Face['identityStatus']) => {
    switch (status) {
      case 'Verified':
        return 'bg-success text-success-foreground';
      case 'Pending':
        return 'bg-warning text-warning-foreground';
      case 'Rejected':
        return 'bg-destructive text-destructive-foreground';
      default:
        return 'bg-muted text-muted-foreground';
    }
  };

  const handleEditFace = (face: Face) => {
    setSelectedFace(face);
    setEditDialogOpen(true);
  };

  const handleUpdateFace = (updatedFace: Face) => {
    setFaces(faces.map(face => 
      face.id === updatedFace.id ? updatedFace : face
    ));
    toast({
      title: 'Face Updated',
      description: `Face #${updatedFace.id} has been updated successfully.`,
    });
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

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
              />
            </div>
            <div className="text-sm text-muted-foreground">
              {filteredFaces.length} of {faces.length} faces
            </div>
          </div>

          {currentFaces.length > 0 ? (
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
                    {currentFaces.map((face) => (
                      <TableRow key={face.id} className="hover:bg-muted/30">
                        <TableCell className="font-medium">#{face.id}</TableCell>
                        <TableCell>
                          <Avatar className="w-10 h-10">
                            <AvatarImage src={face.imageUrl} />
                            <AvatarFallback>
                              <User className="w-4 h-4" />
                            </AvatarFallback>
                          </Avatar>
                        </TableCell>
                        <TableCell>
                          {face.personName ? (
                            <div className="flex items-center gap-2">
                              <User className="w-4 h-4 text-muted-foreground" />
                              <span>{face.personName}</span>
                            </div>
                          ) : (
                            <span className="text-muted-foreground italic">Unassigned</span>
                          )}
                        </TableCell>
                        <TableCell>
                          <Badge className={getStatusColor(face.identityStatus)}>
                            {face.identityStatus}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2 text-sm text-muted-foreground">
                            <Calendar className="w-4 h-4" />
                            {formatDate(face.createdAt)}
                          </div>
                        </TableCell>
                        <TableCell>
                          {face.updatedAt ? (
                            <div className="flex items-center gap-2 text-sm text-muted-foreground">
                              <Calendar className="w-4 h-4" />
                              {formatDate(face.updatedAt)}
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
                          onClick={() => setCurrentPage(Math.max(1, currentPage - 1))}
                          className={currentPage === 1 ? 'pointer-events-none opacity-50' : 'cursor-pointer'}
                        />
                      </PaginationItem>
                      
                      {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                        let pageNum;
                        if (totalPages <= 5) {
                          pageNum = i + 1;
                        } else if (currentPage <= 3) {
                          pageNum = i + 1;
                        } else if (currentPage >= totalPages - 2) {
                          pageNum = totalPages - 4 + i;
                        } else {
                          pageNum = currentPage - 2 + i;
                        }
                        
                        return (
                          <PaginationItem key={pageNum}>
                            <PaginationLink
                              onClick={() => setCurrentPage(pageNum)}
                              isActive={currentPage === pageNum}
                              className="cursor-pointer"
                            >
                              {pageNum}
                            </PaginationLink>
                          </PaginationItem>
                        );
                      })}
                      
                      <PaginationItem>
                        <PaginationNext 
                          onClick={() => setCurrentPage(Math.min(totalPages, currentPage + 1))}
                          className={currentPage === totalPages ? 'pointer-events-none opacity-50' : 'cursor-pointer'}
                        />
                      </PaginationItem>
                    </PaginationContent>
                  </Pagination>
                </div>
              )}
            </>
          ) : (
            <div className="text-center py-12">
              <User className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
              <h3 className="text-lg font-medium text-foreground mb-2">No faces found</h3>
              <p className="text-muted-foreground mb-4">
                {searchTerm ? 'No faces match your search criteria.' : 'No faces have been uploaded yet.'}
              </p>
            </div>
          )}
        </CardContent>
      </Card>

      <EditFaceDialog
        open={editDialogOpen}
        onOpenChange={setEditDialogOpen}
        face={selectedFace}
        onSave={handleUpdateFace}
      />
    </div>
  );
}