import { useEffect, useState } from 'react';
import { FormProvider, useForm } from 'react-hook-form';
import { useAppDispatch, useAppSelector } from '../app/hooks';
import { getPhotos } from '../features/photos/photoSlice';
import { getTags, getPersons, getStorages, getPaths } from '../features/meta/metaSlice';
import PhotoFilters from '@/components/photos/PhotoFilters';
import PhotoSortBar from '@/components/photos/PhotoSortBar';
import ColumnSelector from '@/components/photos/ColumnSelector';
import PhotoTable from '@/components/photos/PhotoTable';
import PhotoCards from '@/components/photos/PhotoCards';
import PaginationControls from '@/components/photos/PaginationControls';
import { useMediaQuery } from '@/lib/hooks/useMediaQuery';

interface FilterForm {
    caption?: string;
    tags?: number[];
    persons?: number[];
    isBW?: boolean;
    isAdultContent?: boolean;
    isRacyContent?: boolean;
    relativePath?: string;
    takenDateFrom?: string;
    takenDateTo?: string;
    thisDay?: boolean;
    orderBy?: string;
    storages?: number[];
    paths?: number[];
}

const defaultColumns = [
    { field: 'thumbnail', label: 'Превью' },
    { field: 'id', label: 'ID' },
    { field: 'name', label: 'Имя' },
    { field: 'takenDate', label: 'Дата' },
    { field: 'isBW', label: 'BW' },
    { field: 'isAdultContent', label: '18+' },
    { field: 'isRacyContent', label: 'Racy' },
    { field: 'tags', label: 'Теги' },
    { field: 'persons', label: 'Люди' },
    { field: 'relativePath', label: 'Путь' },
];

export default function PhotoListPage() {
    const dispatch = useAppDispatch();
    const totalCount = useAppSelector(state => state.photos.count);
    const isMobile = useMediaQuery('(max-width: 768px)');

    const [skip, setSkip] = useState(0);
    const [top] = useState(10);
    const [sortField, setSortField] = useState<string>('takenDate');
    const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');
    const [visibleCols, setVisibleCols] = useState<string[]>(() => {
        const saved = localStorage.getItem('photoVisibleCols');
        return saved ? JSON.parse(saved) : defaultColumns.map(c => c.field);
    });

    const methods = useForm<FilterForm>({
        defaultValues: {
            caption: '',
            tags: [],
            persons: [],
            isBW: undefined,
            isAdultContent: undefined,
            isRacyContent: undefined,
            relativePath: '',
            takenDateFrom: '',
            takenDateTo: '',
            thisDay: false,
            orderBy: '',
            storages: [],
            paths: [],
        },
    });

    const { getValues, reset } = methods;

    const onSubmit = () => {
        setSkip(0);
        const filters = getValues();
        filters.orderBy = `${sortField} ${sortDirection}`;
        localStorage.setItem('photoFilters', JSON.stringify(filters));
        dispatch(getPhotos({ ...filters, skip: 0, top }));
    };

    const onReset = () => {
        reset();
        setSkip(0);
        localStorage.removeItem('photoFilters');
        dispatch(getPhotos({ skip: 0, top }));
    };

    useEffect(() => {
        localStorage.setItem('photoVisibleCols', JSON.stringify(visibleCols));
    }, [visibleCols]);

    useEffect(() => {
        const filters = getValues();
        filters.orderBy = `${sortField} ${sortDirection}`;
        dispatch(getPhotos({ ...filters, skip, top }));
    }, [skip, sortField, sortDirection]);

    useEffect(() => {
        dispatch(getTags());
        dispatch(getPersons());
        dispatch(getStorages());
        dispatch(getPaths());

        const saved = localStorage.getItem('photoFilters');
        if (saved) {
            const parsed = JSON.parse(saved);
            reset(parsed);
        }
    }, [dispatch, reset]);

    return (
        <FormProvider {...methods}>
            <div className="p-4 space-y-4">
                <PhotoFilters onSubmit={onSubmit} onReset={onReset} />
                <PhotoSortBar
                    sortField={sortField}
                    sortDirection={sortDirection}
                    setSortField={setSortField}
                    setSortDirection={setSortDirection}
                />
                <ColumnSelector
                    allColumns={defaultColumns}
                    visibleColumns={visibleCols}
                    setVisibleColumns={setVisibleCols}
                />
                {!isMobile ? (
                    <PhotoTable
                        allColumns={defaultColumns}
                        visibleColumns={visibleCols}
                    />
                ) : (
                    <PhotoCards />
                )}
                <PaginationControls
                    skip={skip}
                    top={top}
                    totalCount={totalCount}
                    setSkip={setSkip}
                />
            </div>
        </FormProvider>
    );
}
