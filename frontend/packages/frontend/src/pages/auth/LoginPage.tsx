import {useNavigate} from 'react-router-dom';
import {useForm} from 'react-hook-form';
import {useState} from 'react';
import {zodResolver} from '@hookform/resolvers/zod';
import {z} from 'zod';
import {login} from '@photobank/shared/api';
import {useAppDispatch} from '@/app/hook.ts';

import {Button} from '@/components/ui/button';
import {Form, FormControl, FormField, FormItem, FormLabel, FormMessage} from '@/components/ui/form';
import {Checkbox} from '@/components/ui/checkbox';
import {Input} from '@/components/ui/input';
import {PasswordInput} from '@/components/ui/password-input';
import {
  loginTitle,
  invalidCredentialsMsg,
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
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const form = useForm<FormData>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      email: '',
      password: '',
      rememberMe: false,
    },
  });

  const onSubmit = async (data: FormData) => {
    try {
      await login(data);
      navigate('/filter');
    } catch (e) {
      console.error(e);
      setErrorMessage(invalidCredentialsMsg);
    }
  };

  return (
    <div className="w-full max-w-sm mx-auto p-4">
      <h1 className="text-2xl font-bold mb-4">{loginTitle}</h1>
      {errorMessage && (
        <p className="text-destructive text-sm mb-2" role="alert">
          {errorMessage}
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
                  <Input {...field} type="email" />
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
                  <PasswordInput {...field} />
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
                    onCheckedChange={field.onChange}
                    id="rememberMe"
                  />
                </FormControl>
                <FormLabel htmlFor="rememberMe" className="font-normal">
                  {stayLoggedInLabel}
                </FormLabel>
              </FormItem>
            )}
          />
          <Button type="submit" className="w-full">{loginButtonText}</Button>
        </form>
      </Form>
    </div>
  );
}
