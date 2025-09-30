import { useEffect, useMemo, useState } from 'react';
import { Loader2 } from 'lucide-react';
import { useQueryClient } from '@tanstack/react-query';
import type { PersonDto, PersonFaceDto, PersonFacesUpdateMutationBody } from '@photobank/shared/api/photobank';
import {
  getPersonFacesGetAllQueryKey,
  usePersonFacesDelete,
  usePersonFacesUpdate,
  usePersonsGetAll,
} from '@photobank/shared/api/photobank';

import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/shared/ui/dialog';
import { Button } from '@/shared/ui/button';
import { Label } from '@/shared/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/shared/ui/select';
import { useToast } from '@/hooks/use-toast';

interface EditFaceDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  face: (PersonFaceDto & {
    identityStatus?: string | null;
    personName?: string | null;
  }) | null;
}

type FormState = Partial<PersonFaceDto> & {
  identityStatus?: string | null;
};

export function EditFaceDialog({ open, onOpenChange, face }: EditFaceDialogProps) {
  const [formData, setFormData] = useState<FormState>({});
  const { toast } = useToast();
  const queryClient = useQueryClient();
  const facesQueryKey = useMemo(() => getPersonFacesGetAllQueryKey(), []);

  const {
    data: personsResponse,
    isLoading: arePersonsLoading,
    isError: hasPersonsError,
    refetch: refetchPersons,
  } = usePersonsGetAll();

  const persons = useMemo<PersonDto[]>(() => personsResponse?.data ?? [], [personsResponse]);

  useEffect(() => {
    if (face) {
      setFormData({
        personId: face.personId ?? undefined,
        identityStatus: face.identityStatus ?? undefined,
      });
    } else {
      setFormData({});
    }
  }, [face]);

  const updateFaceMutation = usePersonFacesUpdate({
    mutation: {
      onError: () => {
        toast({
          title: 'Failed to update face',
          description: 'Something went wrong while saving the changes. Please try again.',
          variant: 'destructive',
        });
      },
    },
  });

  const deleteFaceMutation = usePersonFacesDelete({
    mutation: {
      onError: () => {
        toast({
          title: 'Failed to unassign face',
          description: 'We could not unassign this face. Please try again.',
          variant: 'destructive',
        });
      },
    },
  });

  if (!face) return null;

  const identityStatusOptions = useMemo(() => {
    const baseStatuses = [
      'Pending',
      'Verified',
      'Rejected',
      'Identified',
      'NotIdentified',
      'NotDetected',
      'ForReprocessing',
      'StopProcessing',
    ];

    if (face.identityStatus && !baseStatuses.includes(face.identityStatus)) {
      baseStatuses.push(face.identityStatus);
    }

    return Array.from(new Set(baseStatuses));
  }, [face.identityStatus]);

  const currentIdentityStatus =
    formData.identityStatus ?? face.identityStatus ?? identityStatusOptions[0];

  const pendingPersonSelection = formData.personId ?? face.personId ?? null;

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!face) return;

    const resolvedPersonId = formData.personId ?? face.personId ?? undefined;

    const targetId = face.id ?? face.faceId;
    const faceIdentifier = face.faceId ?? face.id;

    if (targetId == null || faceIdentifier == null) {
      toast({
        title: 'Face information incomplete',
        description: 'Cannot update the selected face because it is missing identifiers.',
        variant: 'destructive',
      });
      return;
    }

    try {
      if (resolvedPersonId == null) {
        await deleteFaceMutation.mutateAsync({ id: targetId });
        toast({
          title: 'Face unassigned',
          description: `Face #${faceIdentifier} has been unassigned from any person.`,
        });
      } else {
        const payload: PersonFacesUpdateMutationBody = {
          id: targetId,
          faceId: faceIdentifier,
          personId: resolvedPersonId,
          provider: formData.provider ?? face.provider,
          externalId: formData.externalId ?? face.externalId,
          externalGuid: formData.externalGuid ?? face.externalGuid,
        } as PersonFacesUpdateMutationBody;

        const extendedPayload = {
          ...payload,
          identityStatus: currentIdentityStatus,
        } as unknown as PersonFacesUpdateMutationBody;

        await updateFaceMutation.mutateAsync({
          id: targetId,
          data: extendedPayload,
        });

        toast({
          title: 'Face updated',
          description: `Face #${faceIdentifier} has been updated successfully.`,
        });
      }

      await queryClient.invalidateQueries({ queryKey: facesQueryKey });
      setFormData({});
      onOpenChange(false);
    } catch {
      // handled in mutation hooks
    }
  };

  const isSaving = updateFaceMutation.isPending || deleteFaceMutation.isPending;
  const isSubmitDisabled =
    isSaving || (arePersonsLoading && persons.length === 0 && pendingPersonSelection !== null);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Edit Face #{face.id ?? face.faceId}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="person">Assign to Person</Label>
            <Select
              value={
                formData.personId != null
                  ? formData.personId.toString()
                  : face.personId != null
                    ? face.personId.toString()
                    : ''
              }
              onValueChange={(value) =>
                setFormData((prev) => ({
                  ...prev,
                  personId: value ? Number.parseInt(value, 10) : undefined,
                }))
              }
              disabled={arePersonsLoading && persons.length === 0}
            >
              <SelectTrigger>
                <SelectValue
                  placeholder={
                    arePersonsLoading
                      ? 'Loading persons…'
                      : 'Select a person or leave unassigned'
                  }
                />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">Unassigned</SelectItem>
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
                onClick={() => refetchPersons()}
              >
                Retry loading persons
              </Button>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="status">Identity Status</Label>
            <Select
              value={currentIdentityStatus ?? ''}
              onValueChange={(value) =>
                setFormData((prev) => ({
                  ...prev,
                  identityStatus: value,
                }))
              }
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {identityStatusOptions.map((status) => (
                  <SelectItem key={status} value={status}>
                    {status}
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
