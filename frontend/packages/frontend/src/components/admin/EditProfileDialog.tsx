import { useEffect, useMemo, useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { format } from 'date-fns';
import { Save, X, Plus, Trash2, ChevronDownIcon } from 'lucide-react';
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
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { Separator } from '@/shared/ui/separator';
import { useToast } from '@/hooks/use-toast';
import { Popover, PopoverContent, PopoverTrigger } from '@/shared/ui/popover';
import { Calendar } from '@/shared/ui/calendar';
import {
  accessProfileFormSchema,
  type AccessProfileFormValues,
} from './accessProfileFormSchema';

interface EditProfileDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  profile: AccessProfileDto | null;
}

type DateRangeFormValue = NonNullable<AccessProfileFormValues['dateRanges']>[number];

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

  const form = useForm<AccessProfileFormValues>({
    resolver: zodResolver(accessProfileFormSchema),
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

      const ranges =
        profile.dateRanges?.flatMap((range) => {
          const fromDateValue = range.fromDate ? new Date(range.fromDate) : null;
          const toDateValue = range.toDate ? new Date(range.toDate) : null;

          if (
            !fromDateValue ||
            Number.isNaN(fromDateValue.getTime()) ||
            !toDateValue ||
            Number.isNaN(toDateValue.getTime())
          ) {
            return [];
          }

          return [
            {
              fromDate: fromDateValue,
              toDate: toDateValue,
            },
          ];
        }) ?? [];

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

  const handleSubmit = async (values: AccessProfileFormValues) => {
    if (!profile?.id) {
      toast({
        title: 'Unable to update profile',
        description: 'This profile is missing an identifier.',
        variant: 'destructive',
      });
      return;
    }

    const normalizedDateRanges =
      values.dateRanges
        ?.filter(
          (range): range is DateRangeFormValue =>
            range.fromDate instanceof Date &&
            !Number.isNaN(range.fromDate.getTime()) &&
            range.toDate instanceof Date &&
            !Number.isNaN(range.toDate.getTime())
        )
        .map(
          (range) =>
            ({
              profileId: profile.id,
              fromDate: format(range.fromDate, 'yyyy-MM-dd') as unknown as Date,
              toDate: format(range.toDate, 'yyyy-MM-dd') as unknown as Date,
            }) satisfies AccessProfileDateRangeAllowDto
        ) ?? [];

    const payload = {
      id: profile.id,
      name: values.name,
      description: values.description || undefined,
      flags_CanSeeNsfw: values.flags_CanSeeNsfw,
      assignedUsersCount: profile.assignedUsersCount,
      storages:
        values.storages?.map((storageId: number) => ({
          profileId: profile.id,
          storageId,
        })) ?? [],
      personGroups:
        values.personGroups?.map((personGroupId: number) => ({
          profileId: profile.id,
          personGroupId,
        })) ?? [],
      dateRanges: normalizedDateRanges,
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
    const oneYearLater = new Date(today);
    oneYearLater.setFullYear(today.getFullYear() + 1);

    const newRange: DateRangeFormValue = {
      fromDate: today,
      toDate: oneYearLater,
    };

    const currentRanges = form.getValues('dateRanges') ?? [];
    const updatedRanges = [...currentRanges, newRange];
    form.setValue('dateRanges', updatedRanges, { shouldValidate: true, shouldDirty: true });
  };

  const removeDateRange = (index: number) => {
    const currentRanges = form.getValues('dateRanges') ?? [];
    const updatedRanges = currentRanges.filter((_, i) => i !== index);
    form.setValue('dateRanges', updatedRanges, { shouldValidate: true, shouldDirty: true });
  };

  const updateDateRange = (index: number, updatedRange: DateRangeFormValue) => {
    const currentRanges = form.getValues('dateRanges') ?? [];
    const nextRanges = [...currentRanges];
    nextRanges[index] = updatedRange;
    form.setValue('dateRanges', nextRanges, { shouldValidate: true, shouldDirty: true });
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
                      <FormLabel>Description</FormLabel>
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
                      <FormLabel>Storages</FormLabel>
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
                                checked={(field.value ?? []).includes(storage.id)}
                                disabled={storagesQuery.isLoading || updateProfileMutation.isPending}
                                onCheckedChange={(checked) => {
                                  const currentValue = field.value ?? [];
                                  if (checked === true) {
                                    field.onChange([...currentValue, storage.id]);
                                  } else {
                                    field.onChange(currentValue.filter((s) => s !== storage.id));
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
                      <FormLabel>Person Groups</FormLabel>
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
                                checked={(field.value ?? []).includes(group.id)}
                                disabled={personGroupsQuery.isLoading || updateProfileMutation.isPending}
                                onCheckedChange={(checked) => {
                                  const currentValue = field.value ?? [];
                                  if (checked === true) {
                                    field.onChange([...currentValue, group.id]);
                                  } else {
                                    field.onChange(currentValue.filter((g) => g !== group.id));
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
                    <Label>Date Ranges</Label>
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
                        <DateRangeRow
                          key={`date-range-${index}`}
                          index={index}
                          range={range}
                          onChange={(nextRange) => updateDateRange(index, nextRange)}
                          onRemove={() => removeDateRange(index)}
                          disabled={updateProfileMutation.isPending}
                        />
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

interface DateRangeRowProps {
  index: number;
  range: DateRangeFormValue;
  onChange: (range: DateRangeFormValue) => void;
  onRemove: () => void;
  disabled: boolean;
}

function DateRangeRow({ index, range, onChange, onRemove, disabled }: DateRangeRowProps) {
  const [openFrom, setOpenFrom] = useState(false);
  const [openTo, setOpenTo] = useState(false);

  const formattedFrom = range.fromDate ? format(range.fromDate, 'yyyy-MM-dd') : 'Select date';
  const formattedTo = range.toDate ? format(range.toDate, 'yyyy-MM-dd') : 'Select date';

  return (
    <div className="flex flex-wrap items-center gap-3 rounded-lg bg-muted/50 p-3">
      <div className="flex flex-col gap-1">
        <span className="text-xs font-medium uppercase text-muted-foreground">From</span>
        <Popover open={openFrom} onOpenChange={setOpenFrom}>
          <PopoverTrigger asChild>
            <Button
              type="button"
              variant="outline"
              className="w-[170px] justify-between font-normal"
              onClick={() => setOpenFrom(true)}
              disabled={disabled}
              aria-label={`Select from date for range ${index + 1}`}
            >
              {formattedFrom}
              <ChevronDownIcon className="ml-2 h-4 w-4 opacity-70" />
            </Button>
          </PopoverTrigger>
          <PopoverContent className="w-auto overflow-hidden p-0" align="start">
            <Calendar
              mode="single"
              selected={range.fromDate}
              captionLayout="dropdown"
              onSelect={(date) => {
                if (!date) {
                  return;
                }

                onChange({ ...range, fromDate: date });
                setOpenFrom(false);
              }}
            />
          </PopoverContent>
        </Popover>
      </div>

      <div className="flex flex-col gap-1">
        <span className="text-xs font-medium uppercase text-muted-foreground">To</span>
        <Popover open={openTo} onOpenChange={setOpenTo}>
          <PopoverTrigger asChild>
            <Button
              type="button"
              variant="outline"
              className="w-[170px] justify-between font-normal"
              onClick={() => setOpenTo(true)}
              disabled={disabled}
              aria-label={`Select to date for range ${index + 1}`}
            >
              {formattedTo}
              <ChevronDownIcon className="ml-2 h-4 w-4 opacity-70" />
            </Button>
          </PopoverTrigger>
          <PopoverContent className="w-auto overflow-hidden p-0" align="start">
            <Calendar
              mode="single"
              selected={range.toDate}
              captionLayout="dropdown"
              onSelect={(date) => {
                if (!date) {
                  return;
                }

                onChange({ ...range, toDate: date });
                setOpenTo(false);
              }}
            />
          </PopoverContent>
        </Popover>
      </div>

      <Button
        type="button"
        variant="ghost"
        size="sm"
        onClick={onRemove}
        className="ml-auto text-destructive hover:text-destructive"
        disabled={disabled}
        aria-label={`Remove date range ${index + 1}`}
      >
        <Trash2 className="h-4 w-4" />
      </Button>
    </div>
  );
}
