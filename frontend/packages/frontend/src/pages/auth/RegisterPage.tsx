import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useAuthRegister } from '@photobank/shared/api/photobank';
import { logger } from '@photobank/shared/utils/logger';
import { useTranslation } from 'react-i18next';
import { ProblemDetailsError } from '@photobank/shared/types/problem';

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

const formSchema = z.object({
  email: z.string().email(),
  password: z.string().min(1),
});

type FormData = z.infer<typeof formSchema>;

export default function RegisterPage() {
  const navigate = useNavigate();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const { mutateAsync: register } = useAuthRegister();
  const form = useForm<FormData>({
    resolver: zodResolver(formSchema),
    defaultValues: { email: '', password: '' },
  });
  const { t } = useTranslation();

  const onSubmit = async (data: FormData) => {
    try {
      await register({ data });
      navigate('/login');
    } catch (e: unknown) {
      if (e instanceof ProblemDetailsError) logger.error(e.problem);
      else logger.error(e);
      setErrorMessage('Failed to register');
    }
  };

  return (
    <div className="w-full max-w-sm mx-auto p-4">
      <h1 className="text-2xl font-bold mb-4">{t('registerTitle')}</h1>
      {errorMessage && (
        <p className="text-destructive text-sm mb-2" role="alert">
          {errorMessage}
        </p>
      )}
      <Form {...form}>
        { }
        <form
          onSubmit={(e) => {
            void form.handleSubmit((data) => onSubmit(data))(e);
          }}
          className="space-y-4"
        >
          <FormField
            control={form.control}
            name="email"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('emailLabel')}</FormLabel>
                <FormControl>
                  <Input {...field} type="email" />
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
                <FormLabel>{t('passwordLabel')}</FormLabel>
                <FormControl>
                  <Input {...field} type="password" />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <Button type="submit" className="w-full">
            {t('registerButtonText')}
          </Button>
        </form>
      </Form>
    </div>
  );
}
