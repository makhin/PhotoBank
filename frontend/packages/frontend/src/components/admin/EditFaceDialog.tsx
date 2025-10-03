import { useEffect, useMemo, useState } from 'react';
import { Loader2 } from 'lucide-react';
import { useQueryClient } from '@tanstack/react-query';
import type { FaceDto, PersonDto } from '@photobank/shared/api/photobank';
import {
  IdentityStatusDto as IdentityStatus,
  type FacesUpdateMutationBody,
  useFacesUpdate,
  usePersonsGetAll,
} from '@photobank/shared/api/photobank';

import { AspectRatio } from '@/shared/ui/aspect-ratio';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/shared/ui/dialog';
import { Button } from '@/shared/ui/button';
import { Label } from '@/shared/ui/label';
import { useToast } from '@/hooks/use-toast';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/ui/select';

const UNASSIGNED_PERSON_VALUE = 'UNASSIGNED';

interface EditFaceDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  face: FaceDto | null;
}

type FormState = {
  faceId: number;
  personId: number | null;
  identityStatus: string;
};

const formatIdentityStatus = (status: string) =>
  status.replace(/([a-z0-9])([A-Z])/g, '$1 $2');

const normalizeIdentityStatus = (
  value: unknown,
  validStatuses: string[]
): string => {
  if (typeof value === 'string' && value.trim()) {
    const match = validStatuses.find(
      (status) => status.toLowerCase() === value.trim().toLowerCase()
    );

    if (match) {
      return match;
    }

    return value;
  }

  return validStatuses[0] ?? IdentityStatus.Undefined;
};

export function EditFaceDialog({ open, onOpenChange, face }: EditFaceDialogProps) {
  const [formData, setFormData] = useState<FormState | null>(null);
  const { toast } = useToast();
  const queryClient = useQueryClient();
  // Use facesQueryKey directly if it's a constant, otherwise import the correct function
  const facesQueryKey = ['faces']; // Replace with actual query key if needed

  const {
    data: personsResponse,
    isLoading: arePersonsLoading,
    isError: hasPersonsError,
    refetch: refetchPersons,
  } = usePersonsGetAll();

  const persons = useMemo<PersonDto[]>(
    () => personsResponse?.data ?? [],
    [personsResponse]
  );

  const identityStatuses = useMemo<string[]>(() => {
    const base: string[] = [...Object.values(IdentityStatus)];

    if (face?.identityStatus) {
      const alreadyPresent = base.some(
        (status) =>
          status.toLowerCase() === String(face.identityStatus).trim().toLowerCase()
      );

      if (!alreadyPresent) {
        base.push(String(face.identityStatus));
      }
    }

    return base;
  }, [face?.identityStatus]);

  useEffect(() => {
    if (!face) {
      setFormData(null);
      return;
    }

    const effectiveFaceId = face.id ?? 0;
    const resolvedPersonId = face.personId ?? null;
    const resolvedIdentityStatus = normalizeIdentityStatus(
      face.identityStatus,
      identityStatuses
    );

    setFormData({
      faceId: effectiveFaceId,
      personId: resolvedPersonId,
      identityStatus: resolvedIdentityStatus,
    });
  }, [face, identityStatuses]);

  const updateFaceMutation = useFacesUpdate({
    mutation: {
      onError: () => {
        toast({
          title: 'Failed to update face',
          description:
            'Something went wrong while saving the changes. Please try again.',
          variant: 'destructive',
        });
      },
    },
  });

  if (!face) return null;

  const imageSrc =
    face.imageUrl ??
    undefined;

  const selectedPersonName = useMemo(() => {
    if (formData?.personId == null) {
      return undefined;
    }
    return persons.find((person) => person.id === formData.personId)?.name;
  }, [formData?.personId, persons]);

  const previewAltText = selectedPersonName
    ? `Face preview for ${selectedPersonName}`
    : `Face preview for face #${face.id ?? formData?.faceId ?? 'unassigned'}`;

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!formData) return;

    const targetFaceId = face.id ?? formData.faceId;

    if (targetFaceId == null) {
      toast({
        title: 'Face information incomplete',
        description: 'Cannot update this face because it has no identifier.',
        variant: 'destructive',
      });
      return;
    }

    const payload = {
      faceId: targetFaceId,
      personId: formData.personId ?? null,
      identityStatus: formData.identityStatus,
    } as FacesUpdateMutationBody;

    try {
      await updateFaceMutation.mutateAsync({ data: payload });
      await queryClient.invalidateQueries({ queryKey: facesQueryKey });

      toast({
        title: formData.personId == null ? 'Face unassigned' : 'Face updated',
        description:
          formData.personId == null
            ? `Face #${targetFaceId} is no longer linked to a person.`
            : `Face #${targetFaceId} has been updated successfully.`,
      });

      onOpenChange(false);
    } catch {
      // handled by mutation error callback
    }
  };

  const isSaving = updateFaceMutation.isPending;
  const isSubmitDisabled = isSaving || arePersonsLoading || !formData;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Edit Face #{face.id ?? formData?.faceId}</DialogTitle>
        </DialogHeader>
        <form onSubmit={(e) => { handleSubmit(e); }} className="space-y-4">
          <div className="space-y-2">
            <p className="text-sm font-medium text-muted-foreground">Face preview</p>
            <AspectRatio ratio={1} className="overflow-hidden rounded-lg border bg-muted/40">
              {imageSrc ? (
                <img
                  src={imageSrc}
                  alt={previewAltText}
                  className="h-full w-full object-cover"
                />
              ) : (
                <div className="flex h-full w-full items-center justify-center text-xs text-muted-foreground">
                  No preview available
                </div>
              )}
            </AspectRatio>
            <p className="text-xs text-muted-foreground">
              {selectedPersonName ?? 'Currently unassigned'}
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="person">Assign to Person</Label>
            <Select
              value={
                formData?.personId != null
                  ? String(formData.personId)
                  : UNASSIGNED_PERSON_VALUE
              }
              onValueChange={(value) =>
                setFormData((prev) => {
                  if (!prev) return prev;

                  if (value === UNASSIGNED_PERSON_VALUE) {
                    return { ...prev, personId: null };
                  }

                  const parsedId = Number(value);

                  if (Number.isNaN(parsedId)) {
                    return prev;
                  }

                  return { ...prev, personId: parsedId };
                })
              }
              disabled={arePersonsLoading || isSaving || !formData}
            >
              <SelectTrigger id="person" className="h-10">
                <SelectValue
                  placeholder={
                    arePersonsLoading ? 'Loading persons...' : 'Select a person'
                  }
                />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={UNASSIGNED_PERSON_VALUE}>Unassigned</SelectItem>
                {persons.map((person) => (
                  <SelectItem key={person.id} value={person.id.toString()}>
                    {person.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            {hasPersonsError && (
              <Button
                type="button"
                variant="link"
                className="px-0 text-xs text-muted-foreground"
                onClick={() => { refetchPersons(); }}
              >
                Retry loading persons
              </Button>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="status">Identity Status</Label>
            <Select
              value={formData?.identityStatus ?? undefined}
              onValueChange={(value) =>
                setFormData((prev) =>
                  prev
                    ? {
                      ...prev,
                      identityStatus: value,
                    }
                    : prev
                )
              }
              disabled={isSaving || !formData}
            >
              <SelectTrigger id="status" className="h-10">
                <SelectValue placeholder="Select a status" />
              </SelectTrigger>
              <SelectContent>
                {identityStatuses.map((status) => (
                  <SelectItem key={status} value={status}>
                    {formatIdentityStatus(status)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="flex justify-end gap-2 pt-4">
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={isSaving}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitDisabled}>
              {isSaving && <Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden="true" />}
              Save Changes
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
