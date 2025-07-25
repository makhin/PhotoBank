import { useEffect, useState } from 'react';
import { getAllUsers, updateUserById, setUserClaims } from '@photobank/shared/api';
import type { UserWithClaimsDto } from '@photobank/shared/generated';
import { Button } from '@/components/ui/button';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { manageUsersTitle, saveUserButtonText, phoneNumberLabel, telegramLabel } from '@photobank/shared/constants';

const schema = z.object({
  phoneNumber: z.string().optional(),
  telegram: z.string().optional(),
  claims: z.string().optional(),
});

type FormData = z.infer<typeof schema>;

interface UserEditorProps {
  user: UserWithClaimsDto;
  onSave: (id: string, data: FormData) => Promise<void>;
}

function UserEditor({ user, onSave }: UserEditorProps) {
  const defaultValues = {
    phoneNumber: user.phoneNumber ?? '',
    telegram: user.telegram ?? '',
    claims: user.claims?.map((c) => `${c.type}:${c.value}`).join('\n') ?? '',
  };
  const form = useForm<FormData>({ resolver: zodResolver(schema), defaultValues });
  return (
    <div className="border p-4 rounded space-y-2">
      <h2 className="font-semibold">{user.email}</h2>
      <Form {...form}>
        {/* eslint-disable-next-line @typescript-eslint/no-misused-promises */}
        <form onSubmit={form.handleSubmit((d) => onSave(user.id!, d))} className="space-y-4">
          <FormField
            control={form.control}
            name="phoneNumber"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{phoneNumberLabel}</FormLabel>
                <FormControl>
                  <Input {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="telegram"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{telegramLabel}</FormLabel>
                <FormControl>
                  <Input {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="claims"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Claims (type:value per line)</FormLabel>
                <FormControl>
                  <Textarea {...field} className="min-h-24" />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <Button type="submit">{saveUserButtonText}</Button>
        </form>
      </Form>
    </div>
  );
}

export default function UsersPage() {
  const [users, setUsers] = useState<UserWithClaimsDto[]>([]);

  useEffect(() => {
    getAllUsers().then(setUsers).catch(console.error);
  }, []);

  const handleSave = async (id: string, data: FormData) => {
    await updateUserById(id, data);
    const claims = (data.claims ?? '').split('\n').filter(Boolean).map((l) => {
      const [type, value] = l.split(':');
      return { type: type.trim(), value: value.trim() };
    });
    if (claims.length) {
      await setUserClaims(id, claims);
    }
  };

  return (
    <div className="max-w-2xl mx-auto p-4 space-y-6">
      <h1 className="text-2xl font-bold">{manageUsersTitle}</h1>
      {users.map((u) => (
        <UserEditor key={u.id} user={u} onSave={handleSave} />
      ))}
    </div>
  );
}
