import {useForm} from 'react-hook-form';
import {zodResolver} from '@hookform/resolvers/zod';
import type {z} from 'zod';
import { useNavigate, useLocation } from 'react-router-dom';
import { useSelector } from 'react-redux';
import { useEffect } from 'react';

import { useAppDispatch, useAppSelector } from '@/app/hook.ts';
import type { RootState } from '@/app/store.ts';
import { setFilter } from '@/features/photo/model/photoSlice.ts';
import { loadMetadata } from '@/features/meta/model/metaSlice.ts';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Form } from '@/components/ui/form';
import { formSchema } from '@/features/filter/lib/form-schema.ts';
import { FilterFormFields } from '@/components/FilterFormFields.tsx';
import { DEFAULT_FORM_VALUES, filterFormTitle, applyFiltersButton, loadingText } from '@photobank/shared/constants';

// Infer FormData type from formSchema to ensure compatibility
type FormData = z.infer<typeof formSchema>;

function FilterPage() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const location = useLocation();
  const savedFilter = useSelector((state: RootState) => state.photo.filter);
  const loaded = useAppSelector((s) => s.metadata.loaded);
  const loading = useAppSelector((s) => s.metadata.loading);

  useEffect(() => {
    if (!loaded && !loading) {
      dispatch(loadMetadata());
    }
  }, [loaded, loading, dispatch]);

  const useCurrentFilter = (location.state as { useCurrentFilter?: boolean } | null)?.useCurrentFilter;

  const savedDefaults: FormData = {
    caption: savedFilter.caption ?? undefined,
    storages: savedFilter.storages?.map(String) ?? [],
    paths: savedFilter.paths?.map(String) ?? [],
    persons: savedFilter.persons?.map(String) ?? [],
    tags: savedFilter.tags?.map(String) ?? [],
    isBW: savedFilter.isBW ?? undefined,
    isAdultContent: savedFilter.isAdultContent ?? undefined,
    isRacyContent: savedFilter.isRacyContent ?? undefined,
    thisDay: savedFilter.thisDay ?? undefined,
    dateFrom: savedFilter.takenDateFrom ? new Date(savedFilter.takenDateFrom) : undefined,
    dateTo: savedFilter.takenDateTo ? new Date(savedFilter.takenDateTo) : undefined,
  };

  const form = useForm<FormData>({
    resolver: zodResolver(formSchema),
    defaultValues: useCurrentFilter ? savedDefaults : { ...DEFAULT_FORM_VALUES },
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
      skip: 0,
      top: 10,
    } as const;

    dispatch(setFilter(filter));
    navigate('/photos');
  };

  if (!loaded) {
    return <p className="p-4">{loadingText}</p>;
  }

  return (
      <Card className="w-full max-w-4xl mx-auto">
        <CardHeader>
          <CardTitle>{filterFormTitle}</CardTitle>
        </CardHeader>
        <CardContent>
          <Form {...form}>
            {/* eslint-disable-next-line @typescript-eslint/no-misused-promises */}
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
              <FilterFormFields control={form.control} />
              {/* Submit Button */}
              <Button type="submit" className="w-full">
                {applyFiltersButton}
              </Button>
            </form>
          </Form>
        </CardContent>
      </Card>
  );
}

export default FilterPage;
