import type { UserDto } from '@photobank/shared/api/photobank';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { useUsersGetAll, useUsersUpdate } from '@photobank/shared/api/photobank';

import { Button } from '@/shared/ui/button';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/shared/ui/form';
import { Input } from '@/shared/ui/input';
const schema = z.object({
  phoneNumber: z.string().optional(),
  telegramUserId: z.string().optional(),
});

type FormData = z.infer<typeof schema>;

interface UserEditorProps {
  user: UserDto & { id: string };
  onSave: (id: string, data: FormData) => Promise<void>;
}

function UserEditor({ user, onSave }: UserEditorProps) {
  const { t } = useTranslation();
  const defaultValues = {
    phoneNumber: user.phoneNumber ?? '',
    telegramUserId: user.telegramUserId ? String(user.telegramUserId) : '',
  };
  const form = useForm<FormData>({ resolver: zodResolver(schema), defaultValues });
  return (
    <div className="border p-4 rounded space-y-2">
      <h2 className="font-semibold">{user.email}</h2>
      <Form {...form}>
        <form
          onSubmit={(e) => {
            void form.handleSubmit((d) => onSave(user.id, d))(e);
          }}
          className="space-y-4"
        >
          <FormField
            control={form.control}
            name="phoneNumber"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('phoneNumberLabel')}</FormLabel>
                <FormControl>
                  <Input {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="telegramUserId"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('telegramLabel')}</FormLabel>
                <FormControl>
                  <Input {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <Button type="submit">{t('saveButtonText')}</Button>
        </form>
      </Form>
    </div>
  );
}

export default function UsersPage() {
  const { data: usersResp } = useUsersGetAll();
  const users = (usersResp?.data ?? []).filter(
    (u): u is UserDto & { id: string } => Boolean(u.id),
  );
  const { mutateAsync: updateUser } = useUsersUpdate();
  const { t } = useTranslation();

  const handleSave = async (id: string, data: FormData) => {
    await updateUser({
      id,
      data: {
        phoneNumber: data.phoneNumber,
        telegramUserId: data.telegramUserId
          ? Number(data.telegramUserId)
          : undefined,
      },
    });
  };

  return (
    <div className="max-w-2xl mx-auto p-4 space-y-6">
      <h1 className="text-2xl font-bold">{t('navbarUsersLabel')}</h1>
      {users.map((u) => (
        <UserEditor key={u.id} user={u} onSave={handleSave} />
      ))}
    </div>
  );
}
