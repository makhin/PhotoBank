import { AccessProfileDialog } from './AccessProfileDialog';

interface CreateProfileDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CreateProfileDialog({ open, onOpenChange }: CreateProfileDialogProps) {
  return (
    <AccessProfileDialog
      mode="create"
      open={open}
      onOpenChange={onOpenChange}
      title="Create Access Profile"
      submitLabel="Create Profile"
      submittingLabel="Creating..."
      successToast={({ values }) => ({
        title: 'Profile created',
        description: values.name
          ? `${values.name} has been successfully created.`
          : 'Profile has been successfully created.',
      })}
      errorToast={{
        title: 'Failed to create profile',
        description:
          'Something went wrong while creating the profile. Please try again.',
        variant: 'destructive',
      }}
    />
  );
}
