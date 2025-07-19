import {useNavigate} from 'react-router-dom';
import {useForm} from 'react-hook-form';
import {useCallback} from 'react';
import {zodResolver} from '@hookform/resolvers/zod';
import {z} from 'zod';
import {loginUser, resetError} from '@/features/auth/model/authSlice.ts';
import {useAppDispatch, useAppSelector} from '@/app/hook.ts';

import {Button} from '@/components/ui/button';
import {Form, FormControl, FormField, FormItem, FormLabel, FormMessage} from '@/components/ui/form';
import {Checkbox} from '@/components/ui/checkbox';
import {Input} from '@/components/ui/input';
import {PasswordInput} from '@/components/ui/password-input';
import {
  loginTitle,
  emailLabel,
  passwordLabel,
  stayLoggedInLabel,
  loginButtonText,
} from '@photobank/shared/constants';

const formSchema = z.object({
  email: z.string().email(),
  password: z.string().min(1),
  rememberMe: z.boolean().optional(),
});

type FormData = z.infer<typeof formSchema>;

export default function LoginPage() {
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const {loading, error} = useAppSelector((s) => s.auth);
  const form = useForm<FormData>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      email: '',
      password: '',
      rememberMe: false,
    },
  });

  const onSubmit = useCallback(
    async (data: FormData) => {
      const result = await dispatch(loginUser(data));
      if (loginUser.fulfilled.match(result)) {
        navigate('/filter');
      }
    },
    [dispatch, navigate],
  );

  const handleFieldChange = useCallback(() => {
    if (error) {
      dispatch(resetError());
    }
  }, [dispatch, error]);

  return (
    <div className="w-full max-w-sm mx-auto p-4">
      <h1 className="text-2xl font-bold mb-4">{loginTitle}</h1>
      {error && (
        <p className="text-destructive text-sm mb-2" role="alert">
          {error}
        </p>
      )}
      <Form {...form}>
        {/* eslint-disable-next-line @typescript-eslint/no-misused-promises */}
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
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
                      field.onChange(e);
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
                      field.onChange(e);
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
                      field.onChange(v);
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
          <Button type="submit" className="w-full" disabled={loading}>
            {loading ? '...' : loginButtonText}
          </Button>
        </form>
      </Form>
    </div>
  );
}
