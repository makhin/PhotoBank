import { useMemo } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { format } from 'date-fns';
import { Save, X, Plus, Trash2 } from 'lucide-react';
import { useQueryClient } from '@tanstack/react-query';
import type {
  AccessProfileDateRangeAllowDto,
  AccessProfileDto,
  AccessProfilePersonGroupAllowDto,
  AccessProfileStorageAllowDto,
} from '@photobank/shared';
import {
  getAdminAccessProfilesListQueryKey,
  useAdminAccessProfilesCreate,
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

import {
  accessProfileFormSchema,
  type AccessProfileFormInput,
  type AccessProfileFormValues,
} from './accessProfileFormSchema';

type AccessProfileDraft = Partial<
  Omit<AccessProfileDto, 'storages' | 'personGroups' | 'dateRanges'>
> & {
  storages?: Array<Partial<AccessProfileStorageAllowDto>>;
  personGroups?: Array<Partial<AccessProfilePersonGroupAllowDto>>;
  dateRanges?: Array<Partial<AccessProfileDateRangeAllowDto>>;
};

type DateRangeInputValue = NonNullable<AccessProfileFormInput['dateRanges']>[number];
type DateRangeFormValue = NonNullable<AccessProfileFormValues['dateRanges']>[number];
type DateRangeDraft = Partial<DateRangeInputValue>;
type CompleteDateRange = DateRangeFormValue & {
  fromDate: Date;
  toDate: Date;
};

const isValidDate = (value: Date | null | undefined): value is Date =>
  value instanceof Date && !Number.isNaN(value.getTime());

const isCompleteDateRange = (
  range: DateRangeFormValue
): range is CompleteDateRange => isValidDate(range.fromDate) && isValidDate(range.toDate);

interface CreateProfileDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CreateProfileDialog({ open, onOpenChange }: CreateProfileDialogProps) {
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

  const form = useForm<AccessProfileFormInput, undefined, AccessProfileFormValues>({
    resolver: zodResolver(accessProfileFormSchema),
    defaultValues: {
      name: '',
      description: '',
      flags_CanSeeNsfw: false,
      storages: [],
      personGroups: [],
      dateRanges: [],
    } satisfies AccessProfileFormInput,
  });

  const createProfileMutation = useAdminAccessProfilesCreate({
    mutation: {
      onSuccess: async (_, variables) => {
        await queryClient.invalidateQueries({ queryKey: accessProfilesQueryKey });

        toast({
          title: 'Profile created',
          description: variables.data.name
            ? `${variables.data.name} has been successfully created.`
            : 'Profile has been successfully created.',
        });

        form.reset();
        onOpenChange(false);
      },
      onError: () => {
        toast({
          title: 'Failed to create profile',
          description: 'Something went wrong while creating the profile. Please try again.',
          variant: 'destructive',
        });
      },
    },
  });

  const handleSubmit = async (values: AccessProfileFormValues) => {
    const normalizedDateRanges =
      values.dateRanges
        ?.filter(isCompleteDateRange)
        .map(
          (range) =>
            ({
              fromDate: format(range.fromDate, 'yyyy-MM-dd') as unknown as Date,
              toDate: format(range.toDate, 'yyyy-MM-dd') as unknown as Date,
            }) satisfies Partial<AccessProfileDateRangeAllowDto>
        ) ?? [];

    const payload: AccessProfileDraft = {
      name: values.name,
      description: values.description || undefined,
      flags_CanSeeNsfw: values.flags_CanSeeNsfw,
      storages: values.storages?.map((storageId) => ({ storageId })) ?? [],
      personGroups: values.personGroups?.map((personGroupId) => ({ personGroupId })) ?? [],
      dateRanges: normalizedDateRanges,
    };

    try {
      await createProfileMutation.mutateAsync({ data: payload as AccessProfileDto });
    } catch {
      // Errors are handled in the mutation callbacks.
    }
  };

  const addDateRange = () => {
    const today = new Date();
    const oneYearLater = new Date();
    oneYearLater.setFullYear(today.getFullYear() + 1);

    const newRange: DateRangeInputValue = {
      fromDate: today,
      toDate: oneYearLater,
    };

    const currentRanges = (form.getValues('dateRanges') ?? []) as DateRangeDraft[];
    const updatedRanges: DateRangeDraft[] = [...currentRanges, newRange];
    form.setValue('dateRanges', updatedRanges as AccessProfileFormInput['dateRanges'], {
      shouldValidate: true,
    });
  };

  const removeDateRange = (index: number) => {
    const currentRanges = (form.getValues('dateRanges') ?? []) as DateRangeDraft[];
    const updatedRanges = currentRanges.filter((_, i) => i !== index);
    form.setValue('dateRanges', updatedRanges as AccessProfileFormInput['dateRanges'], {
      shouldValidate: true,
    });
  };

  const updateDateRange = (
    index: number,
    key: 'fromDate' | 'toDate',
    value: string
  ) => {
    const currentRanges = (form.getValues('dateRanges') ?? []) as DateRangeDraft[];
    const updatedRanges: DateRangeDraft[] = currentRanges.map((range, i) => {
      if (i !== index) {
        return range;
      }

      if (!value) {
        return { ...range, [key]: undefined };
      }

      const parsedValue = new Date(value);

      if (Number.isNaN(parsedValue.getTime())) {
        return range;
      }

      return { ...range, [key]: parsedValue };
    });

    form.setValue('dateRanges', updatedRanges as AccessProfileFormInput['dateRanges'], {
      shouldValidate: true,
      shouldDirty: true,
    });
  };

  const watchedDateRanges = form.watch('dateRanges');
  const dateRanges = (watchedDateRanges ?? []) as DateRangeFormValue[];

  const handleDialogChange = (nextOpen: boolean) => {
    if (!nextOpen) {
      form.reset();
    }

    onOpenChange(nextOpen);
  };

  return (
    <Dialog open={open} onOpenChange={handleDialogChange}>
      <DialogContent className="sm:max-w-[700px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="text-xl font-semibold">Create Access Profile</DialogTitle>
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
                                disabled={storagesQuery.isLoading}
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
                                disabled={personGroupsQuery.isLoading}
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
                      No date ranges defined. Click "Add Range" to add one.
                    </p>
                  ) : (
                    <div className="space-y-2">
                      {dateRanges.map((range, index) => (
                        <div key={index} className="flex flex-col gap-2 p-3 bg-muted/50 rounded-lg md:flex-row md:items-center">
                          <div className="flex flex-1 flex-col gap-1">
                            <Label htmlFor={`date-range-from-${index}`} className="text-xs uppercase tracking-wide text-muted-foreground">
                              From
                            </Label>
                            <Input
                              id={`date-range-from-${index}`}
                              type="date"
                              value={
                                range.fromDate instanceof Date
                                  ? format(range.fromDate, 'yyyy-MM-dd')
                                  : ''
                              }
                              onChange={(event) =>
                                updateDateRange(index, 'fromDate', event.target.value)
                              }
                            />
                          </div>
                          <div className="flex flex-1 flex-col gap-1">
                            <Label htmlFor={`date-range-to-${index}`} className="text-xs uppercase tracking-wide text-muted-foreground">
                              To
                            </Label>
                            <Input
                              id={`date-range-to-${index}`}
                              type="date"
                              value={
                                range.toDate instanceof Date
                                  ? format(range.toDate, 'yyyy-MM-dd')
                                  : ''
                              }
                              onChange={(event) =>
                                updateDateRange(index, 'toDate', event.target.value)
                              }
                            />
                          </div>
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
                disabled={createProfileMutation.isPending}
              >
                <X className="w-4 h-4 mr-2" />
                Cancel
              </Button>
              <Button type="submit" disabled={createProfileMutation.isPending}>
                <Save className="w-4 h-4 mr-2" />
                {createProfileMutation.isPending ? 'Creating...' : 'Create Profile'}
              </Button>
            </div>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
}