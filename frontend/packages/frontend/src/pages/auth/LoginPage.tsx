import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { useCallback, useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  loginTitle,
  emailLabel,
  passwordLabel,
  stayLoggedInLabel,
  loginButtonText,
  invalidCredentialsMsg,
} from '@photobank/shared/constants';
import * as AuthApi from '@photobank/shared/api/photobank/auth/auth';
import { setAuthToken } from '@photobank/shared/auth';

import { Button } from '@/shared/ui/button';
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/shared/ui/form';
import { Checkbox } from '@/shared/ui/checkbox';
import { Input } from '@/shared/ui/input';
import { PasswordInput } from '@/shared/ui/password-input';

const formSchema = z.object({
  email: z.string().email(),
  password: z.string().min(1),
  rememberMe: z.boolean(),
});

type FormData = z.infer<typeof formSchema>;

export default function LoginPage() {
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const form = useForm<FormData>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      email: '',
      password: '',
      rememberMe: false,
    },
  });

  const login = AuthApi.useAuthLogin({
    mutation: {
      onSuccess: (resp, variables) => {
        setAuthToken(resp.data.token! as string, variables.data.rememberMe);
        navigate('/filter');
      },
      onError: () => setError(invalidCredentialsMsg),
    },
  });

  const onSubmit = useCallback(
    (data: FormData) => {
      login.mutate({ data });
    },
    [login],
  );

  const handleFieldChange = useCallback(() => {
    if (error) {
      setError(null);
    }
  }, [error]);

  return (
    <div className="w-full max-w-sm mx-auto p-4">
      <h1 className="text-2xl font-bold mb-4">{loginTitle}</h1>
      {error && (
        <p className="text-destructive text-sm mb-2" role="alert">
          {error}
        </p>
      )}
      <Form {...form}>
        <form
          onSubmit={(e) => {
            void form.handleSubmit((data) => onSubmit(data))(e);
          }}
          className="space-y-4"
        >
          <FormField
            control={form.control}
            name="email"
            render={({field}) => (
              <FormItem>
                <FormLabel>{emailLabel}</FormLabel>
                <FormControl>
                  <Input
                    {...field}
                    type="email"
                    onChange={(e) => {
                      field.onChange(e.target.value);
                      handleFieldChange();
                    }}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="password"
            render={({field}) => (
              <FormItem>
                <FormLabel>{passwordLabel}</FormLabel>
                <FormControl>
                  <PasswordInput
                    {...field}
                    onChange={(e) => {
                      field.onChange(e.target.value);
                      handleFieldChange();
                    }}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="rememberMe"
            render={({field}) => (
              <FormItem className="flex items-center space-x-2">
                <FormControl>
                  <Checkbox
                    checked={field.value}
                    onCheckedChange={(v) => {
                      field.onChange(v === true);
                      handleFieldChange();
                    }}
                    id="rememberMe"
                  />
                </FormControl>
                <FormLabel htmlFor="rememberMe" className="font-normal">
                  {stayLoggedInLabel}
                </FormLabel>
              </FormItem>
            )}
          />
          <Button type="submit" className="w-full" disabled={login.isPending}>
            {login.isPending ? '...' : loginButtonText}
          </Button>
        </form>
      </Form>
    </div>
  );
}
