import { Calendar, User, Tag } from 'lucide-react';
import { useSelector } from 'react-redux';
import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { formatDate } from '@photobank/shared';

import { useSearchPhotosMutation } from '@/entities/photo/api.ts';
import { Badge } from '@/components/ui/badge';
import { Card } from '@/components/ui/card';
import type { PhotoItemDto } from '@photobank/shared/types';
import { ScrollArea } from '@/components/ui/scroll-area';
import type {RootState} from "@/app/store.ts";
import { useAppDispatch } from '@/app/hook.ts';
import { setLastResult } from '@/features/photo/model/photoSlice.ts';
import {MAX_VISIBLE_PERSONS_LG, MAX_VISIBLE_TAGS_LG, MAX_VISIBLE_PERSONS_SM, MAX_VISIBLE_TAGS_SM} from '@/shared/constants';

import PhotoPreview from './PhotoPreview';

const PhotoListPage = () => {
    const dispatch = useAppDispatch();
    const filter  = useSelector((state: RootState) => state.photo.filter);
    const persons = useSelector((state: RootState) => state.metadata.persons);
    const tags    = useSelector((state: RootState) => state.metadata.tags);

    const personsMap = useMemo(() => Object.fromEntries(persons.map(p => [p.id, p.name])), [persons]);
    const tagsMap = useMemo(() => Object.fromEntries(tags.map(t => [t.id, t.name])), [tags]);

    const [searchPhotos] = useSearchPhotosMutation();
    const [photos, setPhotos] = useState<PhotoItemDto[]>([]);
    const navigate = useNavigate();

    useEffect(() => {
        searchPhotos(filter).unwrap().then(result => {
            setPhotos(result.photos || []);
            dispatch(setLastResult(result.photos || []));
        });
    }, [searchPhotos, filter, dispatch]);

    return (
        <div className="w-full h-screen flex flex-col bg-background">
            <div className="p-6 border-b">
                <h1 className="text-3xl font-bold">Photo Gallery</h1>
                <p className="text-muted-foreground mt-2">{photos.length} photos</p>
            </div>

            <ScrollArea className="flex-1">
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
                </div>
            </ScrollArea>
        </div>
    );
};

export default PhotoListPage;