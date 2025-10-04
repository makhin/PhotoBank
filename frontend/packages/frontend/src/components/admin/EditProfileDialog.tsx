import type { AccessProfileDto } from '@photobank/shared';

import { AccessProfileDialog } from './AccessProfileDialog';

interface EditProfileDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  profile: AccessProfileDto | null;
}

export function EditProfileDialog({ open, onOpenChange, profile }: EditProfileDialogProps) {
  return (
    <AccessProfileDialog
      mode="edit"
      open={open}
      onOpenChange={onOpenChange}
      initialProfile={profile}
      title="Edit Access Profile"
      submitLabel="Update Profile"
      submittingLabel="Updating..."
      successToast={({ values }) => ({
        title: 'Profile updated',
        description: values.name
          ? `${values.name} has been successfully updated.`
          : 'Profile has been successfully updated.',
      })}
      errorToast={{
        title: 'Failed to update profile',
        description:
          'Something went wrong while updating the profile. Please try again.',
        variant: 'destructive',
      }}
      missingProfileToast={{
        title: 'Unable to update profile',
        description: 'This profile is missing an identifier.',
        variant: 'destructive',
      }}
    />
  );
}
