import { useMemo } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import type { PersonGroupDto } from '@photobank/shared';
import { useQueryClient } from '@tanstack/react-query';
import {
  getPersonGroupsGetAllQueryKey,
  usePersonGroupsCreate,
} from '@photobank/shared/api/photobank/person-groups/person-groups';

import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/shared/ui/dialog';
import { Button } from '@/shared/ui/button';
import { Input } from '@/shared/ui/input';
import { Textarea } from '@/shared/ui/textarea';
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/shared/ui/form';
import { useToast } from '@/hooks/use-toast';

const createGroupSchema = z.object({
  name: z.string().min(1, 'Group name is required').max(50, 'Group name must be less than 50 characters'),
  description: z.string().min(1, 'Description is required').max(200, 'Description must be less than 200 characters'),
});

type CreateGroupForm = z.infer<typeof createGroupSchema>;

interface CreatePersonGroupDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess?: (group: PersonGroupDto) => void;
  onError?: (error: unknown) => void;
}

export function CreatePersonGroupDialog({
  open,
  onOpenChange,
  onSuccess,
  onError,
}: CreatePersonGroupDialogProps) {
  const { toast } = useToast();
  const queryClient = useQueryClient();
  const personGroupsQueryKey = useMemo(() => getPersonGroupsGetAllQueryKey(), []);

  const form = useForm<CreateGroupForm>({
    resolver: zodResolver(createGroupSchema),
    defaultValues: {
      name: '',
      description: '',
    },
  });

  const createPersonGroupMutation = usePersonGroupsCreate({
    mutation: {
      onSuccess: async (response) => {
        await queryClient.invalidateQueries({ queryKey: personGroupsQueryKey });
        form.reset();
        onOpenChange(false);
        onSuccess?.(response.data);
      },
      onError: (error) => {
        if (onError) {
          onError(error);
        } else {
          toast({
            title: 'Failed to create group',
            description: 'Something went wrong while creating the group. Please try again.',
            variant: 'destructive',
          });
        }
      },
    },
  });

  const onSubmit = async (data: CreateGroupForm) => {
    try {
      await createPersonGroupMutation.mutateAsync({
        data: {
          id: 0,
          name: data.name,
          persons: [],
        },
      });
    } catch {
      // Error handling is managed by the mutation callbacks.
    }
  };

  const handleOpenChange = (nextOpen: boolean) => {
    if (!nextOpen) {
      form.reset();
      createPersonGroupMutation.reset();
    }

    onOpenChange(nextOpen);
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="w-full max-w-md mx-auto">
        <DialogHeader>
          <DialogTitle>Create New Group</DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Group Name</FormLabel>
                  <FormControl>
                    <Input 
                      placeholder="Enter group name"
                      {...field}
                      className="w-full"
                    />
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
                      placeholder="Enter group description"
                      rows={3}
                      {...field}
                      className="w-full resize-none"
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="flex flex-col-reverse sm:flex-row gap-3 pt-4">
              <Button
                type="button"
                variant="outline"
                onClick={() => handleOpenChange(false)}
                disabled={createPersonGroupMutation.isPending}
                className="w-full sm:w-auto"
              >
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={createPersonGroupMutation.isPending}
                className="w-full sm:w-auto"
              >
                {createPersonGroupMutation.isPending ? 'Creating...' : 'Create Group'}
              </Button>
            </div>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
}
