import {useEffect, useState} from 'react';
import {useNavigate} from 'react-router-dom';
import {useForm} from 'react-hook-form';
import {z} from 'zod';
import {zodResolver} from '@hookform/resolvers/zod';
import {
  getCurrentUser,
  updateUser,
  logout,
  getUserRoles,
  getUserClaims,
} from '@photobank/shared/api';

import {Button} from '@/components/ui/button';
import {Form, FormControl, FormField, FormItem, FormLabel, FormMessage} from '@/components/ui/form';
import {Input} from '@/components/ui/input';
import type { UserDto, RoleDto, ClaimDto } from '@photobank/shared/generated';
import {
  myProfileTitle,
  emailPrefix,
  phoneNumberLabel,
  telegramLabel,
  saveButtonText,
  rolesTitle,
  userClaimsTitle,
  logoutButtonText,
} from '@photobank/shared/constants';

const formSchema = z.object({
  phoneNumber: z.string().optional(),
  telegram: z.string().optional(),
});

type FormData = z.infer<typeof formSchema>;

export default function MyProfilePage() {
  const navigate = useNavigate();
  const [user, setUser] = useState<UserDto | null>(null);
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [claims, setClaims] = useState<ClaimDto[]>([]);

  const form = useForm<FormData>({
    resolver: zodResolver(formSchema),
    defaultValues: { phoneNumber: '', telegram: '' },
  });

  useEffect(() => {
    getCurrentUser()
      .then((u) => {
        setUser(u);
        form.reset({ phoneNumber: u.phoneNumber ?? '', telegram: u.telegram ?? '' });
      })
      .catch((e) => {
        console.error(e);
      });
    getUserRoles().then(setRoles).catch(console.error);
    getUserClaims().then(setClaims).catch(console.error);
  }, [form]);

  const onSubmit = async (data: FormData) => {
    try {
      await updateUser(data);
      navigate('/filter');
    } catch (e) {
      console.error(e);
    }
  };

  return (
    <div className="w-full max-w-md mx-auto p-4 space-y-4">
      <h1 className="text-2xl font-bold">{myProfileTitle}</h1>
      {user && (
        <div className="space-y-2">
          <div>
            <span className="font-medium">{emailPrefix}</span> {user.email}
          </div>
          <Form {...form}>
            {/* eslint-disable-next-line @typescript-eslint/no-misused-promises */}
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 mt-4">
              <FormField
                control={form.control}
                name="phoneNumber"
                render={({field}) => (
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
                render={({field}) => (
                  <FormItem>
                    <FormLabel>{telegramLabel}</FormLabel>
                    <FormControl>
                      <Input {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <Button type="submit" className="w-full">{saveButtonText}</Button>
            </form>
          </Form>
          {roles.length > 0 && (
            <div>
              <h2 className="font-medium">{rolesTitle}</h2>
              <ul className="list-disc list-inside space-y-1 ml-4">
                {roles.map((r) => (
                  <li key={r.name}>
                    <span className="font-semibold">{r.name}</span>
                    {r.claims.length > 0 && (
                      <ul className="list-disc list-inside ml-4">
                        {r.claims.map((c, idx) => (
                          <li key={idx}>{c.type}: {c.value}</li>
                        ))}
                      </ul>
                    )}
                  </li>
                ))}
              </ul>
            </div>
          )}
          {claims.length > 0 && (
            <div>
              <h2 className="font-medium">{userClaimsTitle}</h2>
              <ul className="list-disc list-inside ml-4 space-y-1">
                {claims.map((c, idx) => (
                  <li key={idx}>{c.type}: {c.value}</li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}
      <Button variant="secondary" className="w-full" onClick={() => { logout(); navigate('/login'); }}>
        {logoutButtonText}
      </Button>
    </div>
  );
}
