import { useState, useEffect } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/shared/ui/dialog';
import { Button } from '@/shared/ui/button';
import { Input } from '@/shared/ui/input';
import { Label } from '@/shared/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/shared/ui/select';
import { Face, Person } from '@/types/admin';
import { mockPersons } from '@/data/mockData';

interface EditFaceDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  face: Face | null;
  onSave: (face: Face) => void;
}

export function EditFaceDialog({ open, onOpenChange, face, onSave }: EditFaceDialogProps) {
  const [formData, setFormData] = useState<Partial<Face>>({});
  const [availablePersons] = useState<Person[]>(mockPersons);

  useEffect(() => {
    if (face) {
      setFormData({
        ...face,
      });
    } else {
      setFormData({});
    }
  }, [face]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (face && formData) {
      const updatedFace: Face = {
        ...face,
        ...formData,
        personName: formData.personId 
          ? availablePersons.find(p => p.id === formData.personId)?.name
          : undefined,
        updatedAt: new Date().toISOString(),
      };
      onSave(updatedFace);
      onOpenChange(false);
    }
  };

  if (!face) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Edit Face #{face.id}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="person">Assign to Person</Label>
            <Select
              value={formData.personId?.toString() || ''}
              onValueChange={(value) => setFormData({ ...formData, personId: value ? parseInt(value) : undefined })}
            >
              <SelectTrigger>
                <SelectValue placeholder="Select a person or leave unassigned" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">Unassigned</SelectItem>
                {availablePersons.map((person) => (
                  <SelectItem key={person.id} value={person.id.toString()}>
                    {person.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="status">Identity Status</Label>
            <Select
              value={formData.identityStatus || face.identityStatus}
              onValueChange={(value: 'Pending' | 'Verified' | 'Rejected') => 
                setFormData({ ...formData, identityStatus: value })
              }
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Pending">Pending</SelectItem>
                <SelectItem value="Verified">Verified</SelectItem>
                <SelectItem value="Rejected">Rejected</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="flex justify-end gap-2 pt-4">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit">Save Changes</Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}