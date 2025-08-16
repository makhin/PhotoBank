import {useForm} from 'react-hook-form';
import {zodResolver} from '@hookform/resolvers/zod';
import type {z} from 'zod';
import { useNavigate, useLocation, useSearchParams } from 'react-router-dom';
import { useEffect } from 'react';
import { DEFAULT_FORM_VALUES, filterFormTitle, applyFiltersButton, loadingText } from '@photobank/shared/constants';
import * as Api from '@photobank/shared/api/photobank/photos/photos';
import type { FilterDto, PhotoItemDto } from '@photobank/shared/api/photobank';

import { useAppDispatch, useAppSelector } from '@/app/hook';
import { setFilter, setLastResult } from '@/features/photo/model/photoSlice';
import { loadMetadata } from '@/features/meta/model/metaSlice';
import { Button } from '@/shared/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { Form } from '@/shared/ui/form';
import { formSchema } from '@/features/filter/lib/form-schema';
import { FilterFormFields } from '@/components/FilterFormFields';
import { serializeFilter, deserializeFilter } from '@/shared/lib/filter-url';

// Infer FormData type from formSchema to ensure compatibility
type FormData = z.infer<typeof formSchema>;
type PhotosPage = { items?: PhotoItemDto[]; totalCount?: number };

function FilterPage() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const savedFilter = useAppSelector((state) => state.photo.filter);
  const loaded = useAppSelector((s) => s.metadata.loaded);
  const loading = useAppSelector((s) => s.metadata.loading);
  const searchPhotos = Api.usePhotosSearchPhotos();

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

  useEffect(() => {
    const encoded = searchParams.get('filter');
    if (encoded) {
      const parsed = deserializeFilter(encoded);
      if (parsed) {
        form.reset(parsed);
        const filter: FilterDto = {
          caption: parsed.caption,
          storages: parsed.storages?.map(Number),
          paths: parsed.paths?.map(Number),
          persons: parsed.persons?.map(Number),
          tags: parsed.tags?.map(Number),
          isBW: parsed.isBW,
          isAdultContent: parsed.isAdultContent,
          isRacyContent: parsed.isRacyContent,
          thisDay: parsed.thisDay,
          takenDateFrom: parsed.dateFrom?.toISOString(),
          takenDateTo: parsed.dateTo?.toISOString(),
          page: 1,
          pageSize: 10,
        };
        dispatch(setFilter(filter));
      }
    }
  }, [searchParams, dispatch, form]);

  const onSubmit = (data: FormData) => {
    const filter: FilterDto = {
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
      page: 1,
      pageSize: 10,
    };

    const encoded = serializeFilter(data);

    searchPhotos.mutate(
      { data: filter },
      {
        onSuccess: (page) => {
          const photos = (page.data as PhotosPage).items ?? [];
          dispatch(setFilter(filter));
          dispatch(setLastResult(photos));
          navigate(`/photos?filter=${encodeURIComponent(encoded)}`);
        },
        onError: () => {
          // handle error if needed
        },
      }
    );
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
              <form
                onSubmit={(e) => {
                  void form.handleSubmit((d) => onSubmit(d))(e);
                }}
                className="space-y-6"
              >
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
