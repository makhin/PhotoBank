import {useEffect, useState} from 'react';
import {useNavigate} from 'react-router-dom';
import {useForm} from 'react-hook-form';
import {z} from 'zod';
import {zodResolver} from '@hookform/resolvers/zod';
import {getCurrentUser, updateUser, logout} from '@photobank/shared/api';

import {Button} from '@/components/ui/button';
import {Form, FormControl, FormField, FormItem, FormLabel, FormMessage} from '@/components/ui/form';
import {Input} from '@/components/ui/input';
import type {UserDto} from '@photobank/shared/types';

const formSchema = z.object({
  phoneNumber: z.string().optional(),
  telegram: z.string().optional(),
});

type FormData = z.infer<typeof formSchema>;

export default function MyProfilePage() {
  const navigate = useNavigate();
  const [user, setUser] = useState<UserDto | null>(null);

  const form = useForm<FormData>({
    resolver: zodResolver(formSchema),
    defaultValues: { phoneNumber: '', telegram: '' },
  });

  useEffect(() => {
    getCurrentUser().then((u) => {
      setUser(u);
      form.reset({ phoneNumber: u.phoneNumber ?? '', telegram: u.telegram ?? '' });
    }).catch((e) => {
      console.error(e);
    });
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
      <h1 className="text-2xl font-bold">My Profile</h1>
      {user && (
        <div className="space-y-2">
          <div>
            <span className="font-medium">Email:</span> {user.email}
          </div>
          <Form {...form}>
            {/* eslint-disable-next-line @typescript-eslint/no-misused-promises */}
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 mt-4">
              <FormField
                control={form.control}
                name="phoneNumber"
                render={({field}) => (
                  <FormItem>
                    <FormLabel>Phone number</FormLabel>
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
                    <FormLabel>Telegram</FormLabel>
                    <FormControl>
                      <Input {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <Button type="submit" className="w-full">Save</Button>
            </form>
          </Form>
        </div>
      )}
      <Button variant="secondary" className="w-full" onClick={() => { logout(); navigate('/login'); }}>
        Logout
      </Button>
    </div>
  );
}
