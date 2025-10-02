import { useEffect, useMemo, useState } from 'react';
import { Loader2 } from 'lucide-react';
import { useQueryClient } from '@tanstack/react-query';
import type { FaceIdentityDto, PersonDto } from '@photobank/shared/api/photobank';
import {
  getFacesGetQueryKey,
  type FacesUpdateMutationBody,
  IdentityStatus,
  type IdentityStatus as IdentityStatusType,
  useFacesUpdate,
  usePersonsGetAll,
} from '@photobank/shared/api/photobank';

import { AspectRatio } from '@/shared/ui/aspect-ratio';
import { Avatar, AvatarFallback } from '@/shared/ui/avatar';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/shared/ui/dialog';
import { Button } from '@/shared/ui/button';
import { Label } from '@/shared/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/shared/ui/select';
import { useToast } from '@/hooks/use-toast';

const UNASSIGNED_SELECT_VALUE = '__unassigned__';

const DEFAULT_PREVIEW_INITIALS = '??';

const getInitials = (value: string | null | undefined) => {
  if (!value) {
    return DEFAULT_PREVIEW_INITIALS;
  }

  const initials = value
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .map((part) => part[0]?.toUpperCase())
    .filter(Boolean)
    .slice(0, 2)
    .join('');

  return initials || DEFAULT_PREVIEW_INITIALS;
};

export type EditFaceDialogFace = FaceIdentityDto &
  Partial<{
    faceId: number | null;
    personId: number | null;
    personName: string | null;
    imageUrl: string | null;
    image: { url?: string | null } | null;
    createdAt: string | Date | null;
    updatedAt: string | Date | null;
    provider: string | null;
    externalId: string | null;
    externalGuid: string | null;
  }>;

interface EditFaceDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  face: EditFaceDialogFace | null;
}

type FormState = {
  personId?: number | null;
  identityStatus?: string | null;
};

export function EditFaceDialog({ open, onOpenChange, face }: EditFaceDialogProps) {
  const [formData, setFormData] = useState<FormState>({});
  const { toast } = useToast();
  const queryClient = useQueryClient();
  const facesQueryKey = useMemo(() => getFacesGetQueryKey(), []);

  const {
    data: personsResponse,
    isLoading: arePersonsLoading,
    isError: hasPersonsError,
    refetch: refetchPersons,
  } = usePersonsGetAll();

  const persons = useMemo<PersonDto[]>(() => personsResponse?.data ?? [], [personsResponse]);

  useEffect(() => {
    if (face) {
      const existingPersonId =
        face.personId ?? (face.person ? face.person.id ?? null : null);
      setFormData({
        personId: existingPersonId,
        identityStatus: face.identityStatus ?? undefined,
      });
    } else {
      setFormData({});
    }
  }, [face]);

  const updateFaceMutation = useFacesUpdate({
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

  const identityStatusOptions = useMemo(() => {
    const baseStatuses: Array<IdentityStatusType | string> = [
      ...Object.values(IdentityStatus),
    ];

    if (
      face?.identityStatus &&
      !baseStatuses.includes(face.identityStatus as IdentityStatusType)
    ) {
      baseStatuses.push(face.identityStatus);
    }

    return baseStatuses;
  }, [face?.identityStatus]);

  if (!face) return null;

  const imageSrc = face.imageUrl ?? face.image?.url ?? null;
  const personDisplayName = face.personName ?? face.person?.name ?? null;
  const fallbackLabel = personDisplayName ?? 'Unassigned face';
  const previewAltText = personDisplayName
    ? `Face preview for ${personDisplayName}`
    : `Face preview for face #${face.id ?? face.faceId ?? 'unassigned'}`;
  const fallbackInitials = getInitials(fallbackLabel);

  const currentIdentityStatus =
    formData.identityStatus ?? face.identityStatus ?? identityStatusOptions[0] ?? 'Undefined';

  const existingPersonId = face.personId ?? (face.person ? face.person.id ?? null : null);
  const pendingPersonSelection =
    formData.personId !== undefined ? formData.personId : existingPersonId;

  const selectValue = (() => {
    const value = formData.personId !== undefined ? formData.personId : existingPersonId;
    if (value === null || value === undefined) {
      return UNASSIGNED_SELECT_VALUE;
    }

    return value.toString();
  })();

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!face) return;

    const resolvedPersonId =
      formData.personId !== undefined ? formData.personId : existingPersonId;

    const targetFaceId = face.faceId ?? face.id;

    if (targetFaceId == null) {
      toast({
        title: 'Face information incomplete',
        description: 'Cannot update the selected face because it is missing identifiers.',
        variant: 'destructive',
      });
      return;
    }

    try {
      const payload: FacesUpdateMutationBody = {
        faceId: targetFaceId,
        personId: resolvedPersonId ?? null,
        identityStatus: currentIdentityStatus as FacesUpdateMutationBody['identityStatus'],
      };

      await updateFaceMutation.mutateAsync({ data: payload });

      toast({
        title: resolvedPersonId == null ? 'Face unassigned' : 'Face updated',
        description:
          resolvedPersonId == null
            ? `Face #${targetFaceId} has been unassigned from any person.`
            : `Face #${targetFaceId} has been updated successfully.`,
      });

      await queryClient.invalidateQueries({ queryKey: facesQueryKey });
      setFormData({});
      onOpenChange(false);
    } catch {
      // handled in mutation hooks
    }
  };

  const isSaving = updateFaceMutation.isPending;
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
            <p className="text-sm font-medium text-muted-foreground">Face preview</p>
            <AspectRatio ratio={1} className="overflow-hidden rounded-lg border bg-muted/40">
              {imageSrc ? (
                <img
                  src={imageSrc}
                  alt={previewAltText}
                  className="h-full w-full object-cover"
                />
              ) : (
                <div className="flex h-full w-full items-center justify-center bg-background/60">
                  <Avatar className="h-20 w-20">
                    <AvatarFallback className="bg-primary/10 text-lg font-semibold text-primary">
                      {fallbackInitials}
                    </AvatarFallback>
                  </Avatar>
                </div>
              )}
            </AspectRatio>
            <p className="text-xs text-muted-foreground">
              {personDisplayName ?? 'Currently unassigned'}
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="person">Assign to Person</Label>
            <Select
              value={selectValue}
              onValueChange={(value) =>
                setFormData((prev) => ({
                  ...prev,
                  personId:
                    value === UNASSIGNED_SELECT_VALUE ? null : Number.parseInt(value, 10),
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
                <SelectItem value={UNASSIGNED_SELECT_VALUE}>Unassigned</SelectItem>
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
