import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import type { z } from 'zod';
import { useNavigate, useLocation, useSearchParams } from 'react-router-dom';
import { useEffect } from 'react';
import { DEFAULT_FORM_VALUES } from '@photobank/shared/constants';
import { useTranslation } from 'react-i18next';
import type { FilterDto } from '@photobank/shared/api/photobank';
import { createThisDayFilter, withPagination } from '@photobank/shared';
import { parseISO } from 'date-fns';

import { useAppDispatch, useAppSelector } from '@/app/hook';
import { setFilter } from '@/features/photo/model/photoSlice';
import { loadMetadata } from '@/features/meta/model/metaSlice';
import { Button } from '@/shared/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { Form } from '@/shared/ui/form';
import { formSchema } from '@/features/filter/lib/form-schema';
import { FilterFormFields } from '@/components/FilterFormFields';
import { serializeFilter, deserializeFilter } from '@/shared/lib/filter-url';

// Infer FormData type from formSchema to ensure compatibility
type FormData = z.infer<typeof formSchema>;

const mapFormDataToFilter = (data: FormData, now: Date): FilterDto => {
  const thisDay = data.thisDay ? createThisDayFilter(now).thisDay : undefined;

  return withPagination({
    caption: data.caption,
    storages: data.storages?.map(Number),
    paths: data.paths?.map(Number),
    personNames: data.personNames,
    tagNames: data.tagNames,
    isBW: data.isBW,
    isAdultContent: data.isAdultContent,
    isRacyContent: data.isRacyContent,
    thisDay,
    takenDateFrom: data.dateFrom ?? null,
    takenDateTo: data.dateTo ?? null,
  });
};
function FilterPage() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const savedFilter = useAppSelector((state) => state.photo.filter);
  const loaded = useAppSelector((s) => s.metadata.loaded);
  const loading = useAppSelector((s) => s.metadata.loading);
  const error = useAppSelector((s) => s.metadata.error);
  const { t } = useTranslation();

  useEffect(() => {
    if (!loaded && !loading) {
      dispatch(loadMetadata());
    }
  }, [loaded, loading, dispatch]);

  const useCurrentFilter = (location.state as { useCurrentFilter?: boolean } | null)?.useCurrentFilter;

  const ensureDate = (value: unknown): Date | null | undefined => {
    if (value === null) return null;
    if (!value) return undefined;
    if (value instanceof Date) return value;
    if (typeof value === 'string') {
      const parsedDate = parseISO(value);
      return Number.isNaN(parsedDate.getTime()) ? undefined : parsedDate;
    }
    return undefined;
  };

  const savedDefaults: FormData = {
    caption: savedFilter.caption ?? undefined,
    storages: savedFilter.storages?.map(String) ?? [],
    paths: savedFilter.paths?.map(String) ?? [],
    personNames: savedFilter.personNames ?? [],
    tagNames: savedFilter.tagNames ?? [],
    isBW: savedFilter.isBW ?? undefined,
    isAdultContent: savedFilter.isAdultContent ?? undefined,
    isRacyContent: savedFilter.isRacyContent ?? undefined,
    thisDay: savedFilter.thisDay ? true : undefined,
    dateFrom: ensureDate(savedFilter.takenDateFrom),
    dateTo: ensureDate(savedFilter.takenDateTo),
  };

  const form = useForm<FormData>({
    resolver: zodResolver(formSchema),
    defaultValues: useCurrentFilter ? savedDefaults : { ...DEFAULT_FORM_VALUES },
  });

  useEffect(() => {
    const encoded = searchParams.get('filter');
    if (encoded) {
      const parsed = deserializeFilter(encoded);
      if (parsed) {
        form.reset(parsed);
        const filter = mapFormDataToFilter(parsed, new Date());
        dispatch(setFilter(filter));
      }
    }
  }, [searchParams, dispatch, form]);

  const onSubmit = (data: FormData) => {
    const filter = mapFormDataToFilter(data, new Date());

    const encoded = serializeFilter(data);

    dispatch(setFilter(filter));
    navigate(`/photos?filter=${encodeURIComponent(encoded)}`);
  };

  if (!loaded || error) {
    return (
      <p className="p-4">{error ? t('metadataErrorText') : t('loadingText')}</p>
    );
  }

  return (
      <Card className="w-full max-w-4xl mx-auto">
        <CardHeader>
          <CardTitle>{t('filterFormTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
            <Form {...form}>
              <form
                onSubmit={(e) => {
                  void form.handleSubmit((d) => onSubmit(d))(e);
                }}
                className="space-y-6"
              >
                <FilterFormFields control={form.control} />
                {/* Submit Button */}
                <Button type="submit" className="w-full">
                  {t('applyFiltersButton')}
                </Button>
              </form>
            </Form>
        </CardContent>
      </Card>
  );
}

export default FilterPage;
