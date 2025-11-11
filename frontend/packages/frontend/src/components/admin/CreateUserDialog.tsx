import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import * as z from 'zod';
import { Clock, Save, X } from 'lucide-react';
import { useQueryClient } from '@tanstack/react-query';

import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/shared/ui/dialog';
import { Button } from '@/shared/ui/button';
import { Input } from '@/shared/ui/input';
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/ui/select';
import { useToast } from '@/hooks/use-toast';
import {
  useUsersCreate,
  useUsersUpdate,
  getUsersGetAllQueryKey,
} from '@photobank/shared/api/photobank';

const roles = ['Admin', 'User'] as const;

const formSchema = z.object({
  email: z.string().email('Please enter a valid email address'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  phoneNumber: z.string().optional(),
  roles: z.array(z.enum(roles)).min(1, 'At least one role is required'),
  telegramUserId: z
    .string()
    .trim()
    .transform((value) => (value.length === 0 ? '' : value))
    .refine((value) => value === '' || /^\d+$/.test(value), {
      message: 'Telegram ID must contain only digits',
    })
    .optional(),
  telegramSendTimeUtc: z.string().optional(),
});

interface CreateUserDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CreateUserDialog({ open, onOpenChange }: CreateUserDialogProps) {
  const { toast } = useToast();
  const [isLoading, setIsLoading] = useState(false);
  const queryClient = useQueryClient();

  const createUserMutation = useUsersCreate();
  const updateUserMutation = useUsersUpdate();

  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    mode: 'onBlur',
    defaultValues: {
      email: '',
      password: '',
      phoneNumber: '',
      roles: ['User'],
      telegramUserId: '',
      telegramSendTimeUtc: '08:00:00',
    },
  });

  const onSubmit = async (values: z.infer<typeof formSchema>) => {
    console.log('Form submitted with values:', values);
    setIsLoading(true);
    try {
      // Create the user with basic fields
      const createResponse = await createUserMutation.mutateAsync({
        data: {
          email: values.email,
          password: values.password,
          phoneNumber: values.phoneNumber || null,
          roles: values.roles,
        },
      });

      // If telegram fields are provided, update the user
      if (
        createResponse.status === 201 &&
        (values.telegramUserId || values.telegramSendTimeUtc)
      ) {
        const userId = createResponse.data.id;
        if (userId) {
          await updateUserMutation.mutateAsync({
            id: userId,
            data: {
              phoneNumber: values.phoneNumber || null,
              telegramUserId: values.telegramUserId || null,
              telegramSendTimeUtc: values.telegramSendTimeUtc || null,
            },
          });
        }
      }

      // Invalidate and refetch users list
      await queryClient.invalidateQueries({
        queryKey: getUsersGetAllQueryKey(),
      });

      toast({
        title: 'User Created',
        description: `${values.email} has been successfully created.`,
      });

      form.reset();
      onOpenChange(false);
    } catch (error) {
      console.error('Error creating user:', error);
      const errorMessage =
        error instanceof Error ? error.message : 'Failed to create user. Please try again.';
      toast({
        title: 'Error',
        description: errorMessage,
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const onError = (errors: typeof form.formState.errors) => {
    console.error('Form validation errors:', errors);
    const firstError = Object.values(errors)[0];
    toast({
      title: 'Validation Error',
      description: firstError?.message || 'Please check the form for errors.',
      variant: 'destructive',
    });
  };

  const timeOptions = Array.from({ length: 24 }, (_, i) => {
    const hour = i.toString().padStart(2, '0');
    return `${hour}:00:00`;
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="w-[95vw] max-w-[500px] max-h-[90vh] overflow-y-auto p-4 sm:p-6">
        <DialogHeader>
          <DialogTitle className="text-lg sm:text-xl font-semibold">Create New User</DialogTitle>
        </DialogHeader>

        <Form {...form}>
          {/* eslint-disable-next-line @typescript-eslint/no-misused-promises */}
          <form onSubmit={form.handleSubmit(onSubmit, onError)} className="space-y-4">
            <FormField
              control={form.control}
              name="email"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="text-sm font-medium">Email Address *</FormLabel>
                  <FormControl>
                    <Input 
                      placeholder="user@example.com" 
                      className="h-11 text-base"
                      {...field} 
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="password"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="text-sm font-medium">Password *</FormLabel>
                  <FormControl>
                    <Input 
                      type="password" 
                      placeholder="Minimum 8 characters" 
                      className="h-11 text-base"
                      {...field} 
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="phoneNumber"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="text-sm font-medium">Phone Number</FormLabel>
                  <FormControl>
                    <Input 
                      placeholder="+1234567890" 
                      className="h-11 text-base"
                      {...field} 
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="roles"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="text-sm font-medium">Roles *</FormLabel>
                  <div className="flex flex-col sm:flex-row gap-4">
                    {roles.map((role: (typeof roles)[number]) => (
                      <div
                        key={role}
                        className="flex items-center space-x-3 p-3 sm:p-0 rounded-lg sm:rounded-none bg-muted/30 sm:bg-transparent"
                      >
                        <Checkbox
                          id={role}
                          checked={field.value.includes(role)}
                          className="h-5 w-5"
                          onCheckedChange={(checked) => {
                            if (checked) {
                              field.onChange([...field.value, role]);
                            } else {
                              field.onChange(field.value.filter((r) => r !== role));
                            }
                          }}
                        />
                        <Label htmlFor={role} className="text-sm font-medium cursor-pointer flex-1">
                          {role}
                        </Label>
                      </div>
                    ))}
                  </div>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="telegramUserId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="text-sm font-medium">Telegram User ID</FormLabel>
                  <FormControl>
                    <Input
                      inputMode="numeric"
                      pattern="[0-9]*"
                      placeholder="123456789"
                      className="h-11 text-base"
                      value={field.value ?? ''}
                      onChange={(event) => field.onChange(event.target.value)}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="telegramSendTimeUtc"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="text-sm font-medium">Telegram Send Time (UTC)</FormLabel>
                  <Select onValueChange={field.onChange} defaultValue={field.value}>
                    <FormControl>
                      <SelectTrigger className="h-11">
                        <SelectValue placeholder="Select time" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {timeOptions.map((time) => (
                        <SelectItem key={time} value={time}>
                          <div className="flex items-center gap-2">
                            <Clock className="w-4 h-4" />
                            {time}
                          </div>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="flex flex-col-reverse sm:flex-row justify-end gap-3 pt-6">
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
                disabled={isLoading}
                className="w-full sm:w-auto h-12"
                size="lg"
              >
                <X className="w-4 h-4 mr-2" />
                Cancel
              </Button>
              <Button 
                type="submit" 
                disabled={isLoading}
                className="w-full sm:w-auto h-12"
                size="lg"
              >
                <Save className="w-4 h-4 mr-2" />
                {isLoading ? 'Creating...' : 'Create User'}
              </Button>
            </div>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
}