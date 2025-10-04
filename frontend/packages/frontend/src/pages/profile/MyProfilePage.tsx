import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { setAuthToken } from '@photobank/shared/auth';
import { logger } from '@photobank/shared/utils/logger';
import { useTranslation } from 'react-i18next';
import { useAuthGetUser, useAuthUpdateUser } from '@photobank/shared/api/photobank';
import { ProblemDetailsError } from '@photobank/shared/types/problem';

import {Button} from '@/shared/ui/button';
import {Form, FormControl, FormField, FormItem, FormLabel, FormMessage} from '@/shared/ui/form';
import {Input} from '@/shared/ui/input';
import { toast } from '@/shared/ui/sonner';

const formSchema = z.object({
  phoneNumber: z.string().optional(),
  telegramUserId: z.string().optional(),
});

type FormData = z.infer<typeof formSchema>;

export default function MyProfilePage() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { data: userResp } = useAuthGetUser();
  const user = userResp?.data;
  const { mutateAsync: updateUser } = useAuthUpdateUser();

  const form = useForm<FormData>({
    resolver: zodResolver(formSchema),
    defaultValues: { phoneNumber: '', telegramUserId: '' },
  });

  useEffect(() => {
    if (user) {
      form.reset({
        phoneNumber: user.phoneNumber ?? '',
        telegramUserId: user.telegramUserId ? String(user.telegramUserId) : '',
      });
    }
  }, [user, form]);

  const onSubmit = async (data: FormData) => {
    try {
      await updateUser({
        data: {
          phoneNumber: data.phoneNumber,
          telegramUserId: data.telegramUserId
            ? Number(data.telegramUserId)
            : undefined,
        },
      });
      navigate('/filter');
    } catch (e: unknown) {
      const fallbackMessage = t('profileSaveFailed');

      if (e instanceof ProblemDetailsError) {
        const { title, detail } = e.problem;
        const message = title ?? detail ?? fallbackMessage;
        const description =
          detail && detail !== message ? detail : undefined;

        toast.error(message, description ? { description } : undefined);
        logger.error(e.problem);
      } else {
        toast.error(fallbackMessage);
        logger.error(e);
      }
    }
  };

  return (
    <div className="w-full max-w-md mx-auto p-4 space-y-4">
      <h1 className="text-2xl font-bold">{t('myProfileTitle')}</h1>
      {user && (
        <div className="space-y-2">
          <div>
            <span className="font-medium">{t('emailPrefix')}</span> {user.email}
          </div>
          <Form {...form}>
            { }
              <form
                onSubmit={(e) => {
                  void form.handleSubmit((data) => onSubmit(data))(e);
                }}
                className="space-y-4 mt-4"
              >
              <FormField
                control={form.control}
                name="phoneNumber"
                render={({field}) => (
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
                render={({field}) => (
                  <FormItem>
                    <FormLabel>{t('telegramLabel')}</FormLabel>
                    <FormControl>
                      <Input {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <Button type="submit" className="w-full">{t('saveButtonText')}</Button>
            </form>
          </Form>
        </div>
      )}
      <Button
        variant="secondary"
        className="w-full"
        onClick={() => {
          setAuthToken(null, true);
          navigate('/login');
        }}
      >
        {t('logoutButtonText')}
      </Button>
    </div>
  );
}
