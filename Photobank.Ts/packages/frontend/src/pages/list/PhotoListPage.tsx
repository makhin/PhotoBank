import { Calendar, User, Tag } from 'lucide-react';
import { useSelector } from 'react-redux';
import { useEffect, useMemo, useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { formatDate } from '@photobank/shared';

import { useSearchPhotosMutation } from '@/entities/photo/api.ts';
import { Badge } from '@/components/ui/badge';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import type { PhotoItemDto, FilterDto } from '@photobank/shared/types';
import { ScrollArea } from '@/components/ui/scroll-area';
import type { RootState } from '@/app/store.ts';
import { useAppDispatch } from '@/app/hook.ts';
import { setLastResult, setFilter } from '@/features/photo/model/photoSlice.ts';
import {
    MAX_VISIBLE_PERSONS_LG,
    MAX_VISIBLE_TAGS_LG,
    MAX_VISIBLE_PERSONS_SM,
    MAX_VISIBLE_TAGS_SM,
    PHOTOS_CACHE_KEY,
    PHOTOS_CACHE_VERSION,
} from '@/shared/constants';

import PhotoPreview from './PhotoPreview';

interface PhotosCache {
    filter: FilterDto;
    skip: number;
    scrollTop: number;
    version: number;
}

const filterSignature = (f: FilterDto) => {
    const clone = { ...f } as Partial<FilterDto>;
    delete clone.skip;
    delete clone.top;
    return JSON.stringify(clone);
};

const loadCache = (currentFilter: FilterDto): PhotosCache | null => {
    try {
        const raw = localStorage.getItem(PHOTOS_CACHE_KEY);
        if (!raw) return null;
        const parsed: PhotosCache = JSON.parse(raw) as PhotosCache;
        if (parsed.version !== PHOTOS_CACHE_VERSION) return null;
        if (filterSignature(parsed.filter) !== filterSignature(currentFilter)) {
            return null;
        }
        return parsed;
    } catch {
        return null;
    }
};

const saveCache = (data: PhotosCache) => {
    try {
        localStorage.setItem(PHOTOS_CACHE_KEY, JSON.stringify(data));
    } catch {
        console.error('saveCache error');
    }
};

const PhotoListPage = () => {
    const dispatch = useAppDispatch();
    const filter  = useSelector((state: RootState) => state.photo.filter);
    const persons = useSelector((state: RootState) => state.metadata.persons);
    const tags    = useSelector((state: RootState) => state.metadata.tags);

    const personsMap = useMemo(() => Object.fromEntries(persons.map(p => [p.id, p.name])), [persons]);
    const tagsMap = useMemo(() => Object.fromEntries(tags.map(t => [t.id, t.name])), [tags]);

    const [searchPhotos] = useSearchPhotosMutation();
    const [photos, setPhotos] = useState<PhotoItemDto[]>([]);
    const [total, setTotal] = useState(0);
    const [skip, setSkip] = useState(filter.skip ?? 0);
    const top = filter.top ?? 10;
    const navigate = useNavigate();
    const scrollAreaRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const cached = loadCache(filter);
        const activeFilter = cached ? cached.filter : filter;
        if (cached) {
            dispatch(setFilter(cached.filter));
        }

        const initialSkip = cached?.skip ?? 0;
        const queryTop = initialSkip > 0 ? initialSkip : activeFilter.top ?? top;

        searchPhotos({ ...activeFilter, skip: 0, top: queryTop })
            .unwrap()
            .then((result) => {
                const fetched = result.photos || [];
                const newPhotos = fetched.slice(0, initialSkip || fetched.length);
                const newSkip = initialSkip || newPhotos.length;
                setPhotos(newPhotos);
                setTotal(result.count || 0);
                setSkip(newSkip);
                dispatch(setLastResult(newPhotos));
                requestAnimationFrame(() => {
                    const viewport = scrollAreaRef.current?.querySelector(
                        '[data-slot="scroll-area-viewport"]'
                    ) as HTMLElement | null;
                    if (viewport) {
                        viewport.scrollTop = cached?.scrollTop ?? 0;
                    }
                });
                const viewport = scrollAreaRef.current?.querySelector(
                    '[data-slot="scroll-area-viewport"]'
                ) as HTMLElement | null;
                saveCache({
                    filter: activeFilter,
                    skip: newSkip,
                    scrollTop: viewport?.scrollTop ?? 0,
                    version: PHOTOS_CACHE_VERSION,
                });
            });
    }, [searchPhotos, filter, dispatch, top]);

    const loadMore = () => {
        searchPhotos({ ...filter, skip, top }).unwrap().then(result => {
            const newPhotos = result.photos || [];
            const updated = [...photos, ...newPhotos];
            const newSkip = skip + newPhotos.length;
            setPhotos(updated);
            setSkip(newSkip);
            setTotal(result.count || 0);
            dispatch(setLastResult(updated));
            const viewport = scrollAreaRef.current?.querySelector(
                '[data-slot="scroll-area-viewport"]'
            ) as HTMLElement | null;
            saveCache({
                filter,
                skip: newSkip,
                scrollTop: viewport?.scrollTop ?? 0,
                version: PHOTOS_CACHE_VERSION,
            });
        });
    };

    useEffect(() => {
        const handleBeforeUnload = () => {
            const viewport = scrollAreaRef.current?.querySelector(
                '[data-slot="scroll-area-viewport"]'
            ) as HTMLElement | null;
            saveCache({
                filter,
                skip,
                scrollTop: viewport?.scrollTop ?? 0,
                version: PHOTOS_CACHE_VERSION,
            });
        };
        window.addEventListener('beforeunload', handleBeforeUnload);
        return () => {
            handleBeforeUnload();
            window.removeEventListener('beforeunload', handleBeforeUnload);
        };
    }, [filter, skip]);

    return (
        <div className="w-full h-screen flex flex-col bg-background">
            <div className="p-6 border-b flex items-center justify-between">
                <div>
                    <h1 className="text-3xl font-bold">Photo Gallery</h1>
                    <p className="text-muted-foreground mt-2">{photos.length} photos</p>
                </div>
                <Button variant="outline" onClick={() => { navigate('/filter'); }}>
                    Filter
                </Button>
            </div>

            <ScrollArea className="flex-1" ref={scrollAreaRef}>
                <div className="p-6">
                    {/* Desktop/Tablet View */}
                    <div className="hidden lg:block">
                        <div className="grid grid-cols-12 gap-4 mb-4 px-4 py-2 bg-muted/50 rounded-lg font-medium text-sm">
                            <div className="col-span-1">ID</div>
                            <div className="col-span-2">Preview</div>
                            <div className="col-span-2">Name</div>
                            <div className="col-span-1">Date</div>
                            <div className="col-span-2">Storage</div>
                            <div className="col-span-1">Flags</div>
                            <div className="col-span-3">Details</div>
                        </div>

                        <div className="space-y-3">
                            {photos.map((photo) => (
                                // eslint-disable-next-line @typescript-eslint/restrict-template-expressions
                                <Card key={photo.id} className="p-4 hover:shadow-md transition-shadow cursor-pointer" onClick={() => { navigate(`/photos/${photo.id?.toString()}`); }}>
                                    <div className="grid grid-cols-12 gap-4 items-center">
                                        <div className="col-span-1">
                                            <Badge variant="outline" className="font-mono text-xs">
                                                {photo.id || 'N/A'}
                                            </Badge>
                                        </div>

                                        <div className="col-span-2">
                                            <PhotoPreview
                                                thumbnail={photo.thumbnail}
                                                alt={photo.name}
                                                className="w-16 h-16"
                                            />
                                        </div>

                                        <div className="col-span-2">
                                            <div className="font-medium truncate">{photo.name}</div>
                                        </div>

                                        <div className="col-span-1">
                                            <div className="flex items-center gap-1 text-sm">
                                                <Calendar className="w-3 h-3" />
                                                {formatDate(photo.takenDate)}
                                            </div>
                                        </div>

                                        <div className="col-span-2">
                                            <div className="text-xs text-muted-foreground truncate">
                                                {photo.storageName} {photo.relativePath}
                                            </div>
                                        </div>

                                        <div className="col-span-1">
                                            <div className="flex flex-col gap-1">
                                                {photo.isBW && <Badge variant="secondary" className="text-xs">B&W</Badge>}
                                                {photo.isAdultContent && <Badge variant="destructive" className="text-xs">Adult</Badge>}
                                                {photo.isRacyContent && <Badge variant="destructive" className="text-xs">Racy</Badge>}
                                            </div>
                                        </div>

                                        <div className="col-span-3">
                                            <div className="space-y-2">
                                                {photo.persons && photo.persons.length > 0 && (
                                                    <div className="flex items-center gap-1 flex-wrap">
                                                        <User className="w-3 h-3 text-muted-foreground" />
                                                        {photo.persons.slice(0, MAX_VISIBLE_PERSONS_LG).map((person, index) => (
                                                            <Badge key={index} variant="outline" className="text-xs">
                                                                {personsMap[person.personId ?? ''] ?? person.personId}
                                                            </Badge>
                                                        ))}
                                                        {photo.persons.length > MAX_VISIBLE_PERSONS_LG && (
                                                            <Badge variant="outline" className="text-xs">
                                                                +{photo.persons.length - MAX_VISIBLE_PERSONS_LG}
                                                            </Badge>
                                                        )}
                                                    </div>
                                                )}

                                                {photo.tags && photo.tags.length > 0 && (
                                                    <div className="flex items-center gap-1 flex-wrap">
                                                        <Tag className="w-3 h-3 text-muted-foreground" />
                                                        {photo.tags.slice(0, MAX_VISIBLE_TAGS_LG).map((tag, index) => (
                                                            <Badge key={index} variant="secondary" className="text-xs">
                                                                {tagsMap[tag.tagId ?? ''] ?? tag.tagId}
                                                            </Badge>
                                                        ))}
                                                        {photo.tags.length > MAX_VISIBLE_TAGS_LG && (
                                                            <Badge variant="secondary" className="text-xs">
                                                                +{photo.tags.length - MAX_VISIBLE_TAGS_LG}
                                                            </Badge>
                                                        )}
                                                    </div>
                                                )}
                                            </div>
                                        </div>
                                    </div>
                                </Card>
                            ))}
                        </div>
                    </div>

                    {/* Mobile View */}
                    <div className="lg:hidden">
                        <div className="grid gap-4 sm:grid-cols-2">
                            {photos.map((photo) => (
                                // eslint-disable-next-line @typescript-eslint/restrict-template-expressions
                                <Card key={photo.id} className="p-4 hover:shadow-md transition-shadow cursor-pointer" onClick={() => { navigate(`/photos/${photo.id?.toString()}`); }}>
                                    <div className="space-y-3">
                                        <div className="flex items-start gap-3">
                                            <PhotoPreview
                                                thumbnail={photo.thumbnail}
                                                alt={photo.name}
                                                className="w-20 h-20 flex-shrink-0"
                                            />
                                            <div className="flex-1 min-w-0">
                                                <div className="font-medium truncate">{photo.name}</div>
                                                <Badge variant="outline" className="font-mono text-xs mt-1">
                                                    {photo.id || 'N/A'}
                                                </Badge>
                                            </div>
                                        </div>

                                        <div className="text-xs text-muted-foreground">
                                            {photo.storageName} {photo.relativePath}
                                        </div>

                                        <div className="flex items-center gap-4 text-sm">
                                            <div className="flex items-center gap-1">
                                                <Calendar className="w-3 h-3" />
                                                {formatDate(photo.takenDate)}
                                            </div>
                                        </div>

                                        <div className="flex flex-wrap gap-1">
                                            {photo.isBW && <Badge variant="secondary" className="text-xs">B&W</Badge>}
                                            {photo.isAdultContent && <Badge variant="destructive" className="text-xs">Adult</Badge>}
                                            {photo.isRacyContent && <Badge variant="destructive" className="text-xs">Racy</Badge>}
                                        </div>

                                        {photo.persons && photo.persons.length > 0 && (
                                            <div className="flex items-center gap-1 flex-wrap">
                                                <User className="w-3 h-3 text-muted-foreground" />
                                                {photo.persons.slice(0, MAX_VISIBLE_PERSONS_SM).map((person, index) => (
                                                    <Badge key={index} variant="outline" className="text-xs">
                                                        {personsMap[person.personId ?? ''] ?? person.personId}
                                                    </Badge>
                                                ))}
                                                {photo.persons.length > MAX_VISIBLE_PERSONS_SM && (
                                                    <Badge variant="outline" className="text-xs">
                                                        +{photo.persons.length - MAX_VISIBLE_PERSONS_SM}
                                                    </Badge>
                                                )}
                                            </div>
                                        )}

                                        {photo.tags && photo.tags.length > 0 && (
                                            <div className="flex items-center gap-1 flex-wrap">
                                                <Tag className="w-3 h-3 text-muted-foreground" />
                                                {photo.tags.slice(0, MAX_VISIBLE_TAGS_SM).map((tag, index) => (
                                                    <Badge key={index} variant="secondary" className="text-xs">
                                                        {tagsMap[tag.tagId ?? ''] ?? tag.tagId}
                                                    </Badge>
                                                ))}
                                                {photo.tags.length > MAX_VISIBLE_TAGS_SM && (
                                                    <Badge variant="secondary" className="text-xs">
                                                        +{photo.tags.length - MAX_VISIBLE_TAGS_SM}
                                                    </Badge>
                                                )}
                                            </div>
                                        )}
                                    </div>
                                </Card>
                            ))}
                        </div>
                    </div>
                    {photos.length < total && (
                        <div className="flex justify-center mt-4">
                            <Button variant="outline" onClick={loadMore}>Load More</Button>
                        </div>
                    )}
                </div>
            </ScrollArea>
        </div>
    );
};

export default PhotoListPage;