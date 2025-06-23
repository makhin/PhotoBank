import {useForm} from 'react-hook-form';
import {zodResolver} from '@hookform/resolvers/zod';
import type {z} from 'zod';

import {Button} from '@/components/ui/button';
import {Card, CardContent, CardHeader, CardTitle} from '@/components/ui/card';
import {Form} from '@/components/ui/form';
import {formSchema} from '@/features/filter/lib/form-schema.ts';
import {FilterFormFields} from '@/components/FilterFormFields.tsx';

// Infer FormData type from formSchema to ensure compatibility
type FormData = z.infer<typeof formSchema>;

export function FilterForm() {
  const form = useForm<FormData>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      caption: '',
      storages: [],
      paths: [],
      persons: [],
      tags: [],
      isBW: false,
      isAdultContent: false,
      isRacyContent: false,
      thisDay: true,
      dateRange: {
        from: null,
        to: null,
      },
    },
  });

  const onSubmit = (data: FormData) => {
    console.log('Form submitted:', data);
  };

  return (
    <Card className="w-full max-w-4xl mx-auto">
      <CardHeader>
        <CardTitle>Advanced Filter Form</CardTitle>
      </CardHeader>
      <CardContent>
        <Form {...form}>
          {/* eslint-disable-next-line @typescript-eslint/no-misused-promises */}
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
            <FilterFormFields control={form.control} />
            {/* Submit Button */}
            <Button type="submit" className="w-full">
              Apply Filters
            </Button>
          </form>
        </Form>
      </CardContent>
    </Card>
  );
}
