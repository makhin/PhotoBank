import {useForm} from 'react-hook-form';
import {zodResolver} from '@hookform/resolvers/zod';
import type {z} from 'zod';

import { useNavigate } from 'react-router-dom';
import { useAppDispatch } from '@/app/hook.ts';
import { setFilter } from '@/features/photo/model/photoSlice.ts';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Form } from '@/components/ui/form';
import { formSchema } from '@/features/filter/lib/form-schema.ts';
import { FilterFormFields } from '@/components/FilterFormFields.tsx';

// Infer FormData type from formSchema to ensure compatibility
type FormData = z.infer<typeof formSchema>;

function FilterPage() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  const form = useForm<FormData>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      caption: undefined,
      storages: [],
      paths: [],
      persons: [],
      tags: [],
      isBW: undefined,
      isAdultContent: undefined,
      isRacyContent: undefined,
      thisDay: true,
      dateFrom: undefined,
      dateTo: undefined,
    },
  });

  const onSubmit = (data: FormData) => {
    const filter = {
      caption: data.caption,
      storages: data.storages?.map(Number),
      paths: data.paths?.map(Number),
      persons: data.persons?.map(Number),
      tags: data.tags?.map(Number),
      isBW: data.isBW,
      isAdultContent: data.isAdultContent,
      isRacyContent: data.isRacyContent,
      thisDay: data.thisDay,
      takenDateFrom: data.dateFrom?.toISOString(),
      takenDateTo: data.dateTo?.toISOString(),
    } as const;

    dispatch(setFilter(filter));
    navigate('/photos');
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

export default FilterPage;
