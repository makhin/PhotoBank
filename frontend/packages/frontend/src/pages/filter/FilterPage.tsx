import {useForm} from 'react-hook-form';
import {zodResolver} from '@hookform/resolvers/zod';
import type {z} from 'zod';
import { useNavigate, useLocation, useSearchParams } from 'react-router-dom';
import { useEffect, useMemo } from 'react';
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
import {
  serializeFilter,
  deserializeFilter,
  formDataToFilterDto,
  filterDtoToFormData,
} from '@/shared/lib/filter-url';

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

  const urlFilter = useMemo(() => {
    const encoded = searchParams.get('filter');
    return encoded ? deserializeFilter(encoded) : null;
  }, [searchParams]);

  useEffect(() => {
    if (urlFilter) {
      dispatch(setFilter(urlFilter));
    }
  }, [urlFilter, dispatch]);

  const savedDefaults: FormData = filterDtoToFormData(savedFilter);
  const urlDefaults: FormData | null = urlFilter
    ? filterDtoToFormData(urlFilter)
    : null;

  const defaultValues = useCurrentFilter
    ? savedDefaults
    : urlDefaults ?? { ...DEFAULT_FORM_VALUES };

  const form = useForm<FormData>({
    resolver: zodResolver(formSchema),
    defaultValues,
  });

  const onSubmit = (data: FormData) => {
    const filter: FilterDto = formDataToFilterDto(data);
    const encoded = serializeFilter(data);

    searchPhotos.mutate(
      { data: filter },
      {
        onSuccess: (page) => {
          const photos = (page.data as PhotosPage).items ?? [];
          dispatch(setFilter(filter));
          dispatch(setLastResult(photos));
          navigate({ pathname: '/photos', search: `?filter=${encoded}` });
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
