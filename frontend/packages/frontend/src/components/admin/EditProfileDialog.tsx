import { useEffect, useMemo } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import * as z from 'zod';
import { format } from 'date-fns';
import { Save, X, Plus, Trash2 } from 'lucide-react';
import type {
  AccessProfileDateRangeAllowDto,
  AccessProfileDto,
  AccessProfilePersonGroupAllowDto,
  AccessProfileStorageAllowDto,
} from '@photobank/shared';
import { useQueryClient } from '@tanstack/react-query';
import {
  getAdminAccessProfilesListQueryKey,
  useAdminAccessProfilesUpdate,
} from '@photobank/shared/api/photobank/admin-access-profiles/admin-access-profiles';
import { useGetStorages } from '@photobank/shared/api/photobank/storages/storages';
import { usePersonGroupsGetAll } from '@photobank/shared/api/photobank/person-groups/person-groups';

import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/shared/ui/dialog';
import { Button } from '@/shared/ui/button';
import { Input } from '@/shared/ui/input';
import { Textarea } from '@/shared/ui/textarea';
import { Label } from '@/shared/ui/label';
import { Checkbox } from '@/shared/ui/checkbox';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/shared/ui/form';
import { Badge } from '@/shared/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { Separator } from '@/shared/ui/separator';
import { useToast } from '@/hooks/use-toast';

const formSchema = z.object({
  name: z.string().min(1, 'Profile name is required').max(128, 'Name must be 128 characters or less'),
  description: z.string().min(1, 'Description is required').max(512, 'Description must be 512 characters or less'),
  flags_CanSeeNsfw: z.boolean(),
  storages: z.array(z.number()).min(1, 'At least one storage is required'),
  personGroups: z.array(z.number()).min(1, 'At least one person group is required'),
  dateRanges: z.array(z.object({
    fromDate: z.string(),
    toDate: z.string(),
  })).min(1, 'At least one date range is required'),
});

interface EditProfileDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  profile: AccessProfileDto | null;
}

export function EditProfileDialog({ open, onOpenChange, profile }: EditProfileDialogProps) {
  const { toast } = useToast();
  const queryClient = useQueryClient();
  const accessProfilesQueryKey = useMemo(
    () => getAdminAccessProfilesListQueryKey(),
    []
  );

  const storagesQuery = useGetStorages();
  const storages = useMemo(() => storagesQuery.data?.data ?? [], [storagesQuery.data]);

  const personGroupsQuery = usePersonGroupsGetAll();
  const personGroups = useMemo(
    () => personGroupsQuery.data?.data ?? [],
    [personGroupsQuery.data]
  );

  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      name: '',
      description: '',
      flags_CanSeeNsfw: false,
      storages: [],
      personGroups: [],
      dateRanges: [],
    },
  });

  // Populate form when profile changes
  useEffect(() => {
    if (profile && open) {
      const storageIds = profile.storages?.flatMap((storage) =>
        typeof storage.storageId === 'number' ? [storage.storageId] : []
      ) ?? [];

      const personGroupIds = profile.personGroups?.flatMap((group) =>
        typeof group.personGroupId === 'number' ? [group.personGroupId] : []
      ) ?? [];

      const ranges = profile.dateRanges?.map((range) => ({
        fromDate: range.fromDate
          ? format(new Date(range.fromDate), 'yyyy-MM-dd')
          : '',
        toDate: range.toDate ? format(new Date(range.toDate), 'yyyy-MM-dd') : '',
      })) ?? [];

      form.reset({
        name: profile.name ?? '',
        description: profile.description ?? '',
        flags_CanSeeNsfw: profile.flags_CanSeeNsfw ?? false,
        storages: storageIds,
        personGroups: personGroupIds,
        dateRanges: ranges,
      });
    }
  }, [profile, open, form]);

  const updateProfileMutation = useAdminAccessProfilesUpdate({
    mutation: {
      onSuccess: async (_, variables) => {
        await queryClient.invalidateQueries({ queryKey: accessProfilesQueryKey });

        toast({
          title: 'Profile updated',
          description: variables.data.name
            ? `${variables.data.name} has been successfully updated.`
            : 'Profile has been successfully updated.',
        });
      },
      onError: () => {
        toast({
          title: 'Failed to update profile',
          description: 'Something went wrong while updating the profile. Please try again.',
          variant: 'destructive',
        });
      },
      onSettled: () => {
        form.reset();
        onOpenChange(false);
      },
    },
  });

  const handleSubmit = async (values: z.infer<typeof formSchema>) => {
    if (!profile?.id) {
      toast({
        title: 'Unable to update profile',
        description: 'This profile is missing an identifier.',
        variant: 'destructive',
      });
      return;
    }

    const payload = {
      id: profile.id,
      name: values.name,
      description: values.description,
      flags_CanSeeNsfw: values.flags_CanSeeNsfw,
      storages: values.storages.map((storageId) => ({
        profileId: profile.id,
        storageId,
      })),
      personGroups: values.personGroups.map((personGroupId) => ({
        profileId: profile.id,
        personGroupId,
      })),
      dateRanges: values.dateRanges.map((range) => ({
        profileId: profile.id,
        fromDate: new Date(range.fromDate),
        toDate: new Date(range.toDate),
      })),
    } satisfies AccessProfileDto & {
      storages: AccessProfileStorageAllowDto[];
      personGroups: AccessProfilePersonGroupAllowDto[];
      dateRanges: AccessProfileDateRangeAllowDto[];
    };

    try {
      await updateProfileMutation.mutateAsync({ id: profile.id, data: payload });
    } catch {
      // Error handling is managed by the mutation callbacks.
    }
  };

  const addDateRange = () => {
    const today = new Date();
    const oneYearLater = new Date();
    oneYearLater.setFullYear(today.getFullYear() + 1);

    const newRange = {
      fromDate: format(today, 'yyyy-MM-dd'),
      toDate: format(oneYearLater, 'yyyy-MM-dd'),
    };

    const currentRanges = form.getValues('dateRanges');
    const updatedRanges = [...currentRanges, newRange];
    form.setValue('dateRanges', updatedRanges, { shouldValidate: true });
  };

  const removeDateRange = (index: number) => {
    const currentRanges = form.getValues('dateRanges');
    const updatedRanges = currentRanges.filter((_, i) => i !== index);
    form.setValue('dateRanges', updatedRanges, { shouldValidate: true });
  };

  const dateRanges = form.watch('dateRanges') ?? [];

  const handleDialogChange = (nextOpen: boolean) => {
    if (!nextOpen) {
      if (updateProfileMutation.isPending) {
        return;
      }

      form.reset();
    }

    onOpenChange(nextOpen);
  };

  return (
    <Dialog open={open} onOpenChange={handleDialogChange}>
      <DialogContent className="sm:max-w-[700px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="text-xl font-semibold">Edit Access Profile</DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-6">
            {/* General Information */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">General Information</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <FormField
                  control={form.control}
                  name="name"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Profile Name *</FormLabel>
                      <FormControl>
                        <Input placeholder="e.g., Editors – Summer" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="description"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Description *</FormLabel>
                      <FormControl>
                        <Textarea 
                          placeholder="Describe the purpose and scope of this access profile..."
                          {...field} 
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="flags_CanSeeNsfw"
                  render={({ field }) => (
                    <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3 shadow-sm">
                      <div className="space-y-0.5">
                        <FormLabel>NSFW Content Access</FormLabel>
                        <div className="text-[0.8rem] text-muted-foreground">
                          Allow access to adult or sensitive content
                        </div>
                      </div>
                      <FormControl>
                        <Checkbox
                          checked={field.value}
                          onCheckedChange={(checked) => field.onChange(checked === true)}
                        />
                      </FormControl>
                    </FormItem>
                  )}
                />
              </CardContent>
            </Card>

            {/* Access Rules */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Access Rules</CardTitle>
              </CardHeader>
              <CardContent className="space-y-6">
                {/* Storages */}
                <FormField
                  control={form.control}
                  name="storages"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Storages *</FormLabel>
                      <div className="grid grid-cols-2 gap-2">
                        {storages.length === 0 ? (
                          <p className="col-span-2 text-sm text-muted-foreground">
                            {storagesQuery.isLoading
                              ? 'Loading storages...'
                              : 'No storages available yet.'}
                          </p>
                        ) : (
                          storages.map((storage) => (
                            <div key={storage.id} className="flex items-center space-x-2">
                            <Checkbox
                              id={`storage-${storage.id}`}
                              checked={field.value.includes(storage.id)}
                              disabled={storagesQuery.isLoading || updateProfileMutation.isPending}
                              onCheckedChange={(checked) => {
                                if (checked === true) {
                                  field.onChange([...field.value, storage.id]);
                                } else {
                                  field.onChange(field.value.filter((s) => s !== storage.id));
                                }
                              }}
                            />
                            <Label htmlFor={`storage-${storage.id}`} className="text-sm">
                              {storage.name}
                            </Label>
                          </div>
                          ))
                        )}
                      </div>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <Separator />

                {/* Person Groups */}
                <FormField
                  control={form.control}
                  name="personGroups"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Person Groups *</FormLabel>
                      <div className="grid grid-cols-2 gap-2">
                        {personGroups.length === 0 ? (
                          <p className="col-span-2 text-sm text-muted-foreground">
                            {personGroupsQuery.isLoading
                              ? 'Loading person groups...'
                              : 'No person groups available yet.'}
                          </p>
                        ) : (
                          personGroups.map((group) => (
                            <div key={group.id} className="flex items-center space-x-2">
                              <Checkbox
                                id={`group-${group.id}`}
                                checked={field.value.includes(group.id)}
                                disabled={personGroupsQuery.isLoading || updateProfileMutation.isPending}
                                onCheckedChange={(checked) => {
                                  if (checked === true) {
                                    field.onChange([...field.value, group.id]);
                                  } else {
                                    field.onChange(field.value.filter((g) => g !== group.id));
                                  }
                                }}
                              />
                              <Label htmlFor={`group-${group.id}`} className="text-sm">
                                {group.name}
                              </Label>
                            </div>
                          ))
                        )}
                      </div>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <Separator />

                {/* Date Ranges */}
                <div>
                  <div className="flex items-center justify-between mb-3">
                    <Label>Date Ranges *</Label>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={addDateRange}
                    >
                      <Plus className="w-4 h-4 mr-2" />
                      Add Range
                    </Button>
                  </div>
                  
                  {dateRanges.length === 0 ? (
                    <p className="text-sm text-muted-foreground text-center py-4 border border-dashed rounded-lg">
                      No date ranges defined. Click &ldquo;Add Range&rdquo; to add one.
                    </p>
                  ) : (
                    <div className="space-y-2">
                      {dateRanges.map((range, index) => (
                        <div key={index} className="flex items-center gap-2 p-3 bg-muted/50 rounded-lg">
                          <span className="text-sm font-medium">From:</span>
                          <Badge variant="outline">{range.fromDate}</Badge>
                          <span className="text-sm font-medium">To:</span>
                          <Badge variant="outline">{range.toDate}</Badge>
                          <Button
                            type="button"
                            variant="ghost"
                            size="sm"
                            onClick={() => removeDateRange(index)}
                            className="ml-auto text-destructive hover:text-destructive"
                          >
                            <Trash2 className="w-4 h-4" />
                          </Button>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>

            <div className="flex justify-end gap-2 pt-4">
              <Button
                type="button"
                variant="outline"
                onClick={() => handleDialogChange(false)}
                disabled={updateProfileMutation.isPending}
              >
                <X className="w-4 h-4 mr-2" />
                Cancel
              </Button>
              <Button type="submit" disabled={updateProfileMutation.isPending}>
                <Save className="w-4 h-4 mr-2" />
                {updateProfileMutation.isPending ? 'Updating...' : 'Update Profile'}
              </Button>
            </div>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
}