import { Calendar, User, Tag } from 'lucide-react';
import {useSelector} from "react-redux";
import {useEffect, useState} from "react";
import { useNavigate } from "react-router-dom";

import { useSearchPhotosMutation } from '@/entities/photo/api.ts';
import { Badge } from '@/components/ui/badge';
import { Card } from '@/components/ui/card';
import type { PhotoItemDto } from '@/entities/photo/model';
import { ScrollArea } from '@/components/ui/scroll-area';
import type {RootState} from "@/app/store.ts";
import { formatDate } from '@/lib/utils';

import PhotoPreview from './PhotoPreview';

const PhotoListPage = () => {
    const persons = useSelector((state: RootState) => state.metadata.persons);
    const tags    = useSelector((state: RootState) => state.metadata.tags);

    const [searchPhotos] = useSearchPhotosMutation();
    const [photos, setPhotos] = useState<PhotoItemDto[]>([]);
    const navigate = useNavigate();

    useEffect(() => {
        searchPhotos({thisDay: true, orderBy: 'takenDate', top: 50}).unwrap().then(result => {
            setPhotos(result.photos || []);
        });
    }, [searchPhotos]);

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
                                                        {photo.persons.slice(0, 3).map((person, index) => (
                                                            <Badge key={index} variant="outline" className="text-xs">
                                                                { persons.find(p => p.id === person.personId)?.name || person.personId }
                                                            </Badge>
                                                        ))}
                                                        {photo.persons.length > 3 && (
                                                            <Badge variant="outline" className="text-xs">
                                                                +{photo.persons.length - 3}
                                                            </Badge>
                                                        )}
                                                    </div>
                                                )}

                                                {photo.tags && photo.tags.length > 0 && (
                                                    <div className="flex items-center gap-1 flex-wrap">
                                                        <Tag className="w-3 h-3 text-muted-foreground" />
                                                        {photo.tags.slice(0, 3).map((tag, index) => (
                                                            <Badge key={index} variant="secondary" className="text-xs">
                                                                {tags.find(t => t.id === tag.tagId)?.name || tag.tagId}
                                                            </Badge>
                                                        ))}
                                                        {photo.tags.length > 3 && (
                                                            <Badge variant="secondary" className="text-xs">
                                                                +{photo.tags.length - 3}
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
                                                {photo.persons.slice(0, 2).map((person, index) => (
                                                    <Badge key={index} variant="outline" className="text-xs">
                                                        {persons.find(p => p.id === person.personId)?.name || person.personId}
                                                    </Badge>
                                                ))}
                                                {photo.persons.length > 2 && (
                                                    <Badge variant="outline" className="text-xs">
                                                        +{photo.persons.length - 2}
                                                    </Badge>
                                                )}
                                            </div>
                                        )}

                                        {photo.tags && photo.tags.length > 0 && (
                                            <div className="flex items-center gap-1 flex-wrap">
                                                <Tag className="w-3 h-3 text-muted-foreground" />
                                                {photo.tags.slice(0, 2).map((tag, index) => (
                                                    <Badge key={index} variant="secondary" className="text-xs">
                                                        {tags.find(t => t.id === tag.tagId)?.name || tag.tagId}
                                                    </Badge>
                                                ))}
                                                {photo.tags.length > 2 && (
                                                    <Badge variant="secondary" className="text-xs">
                                                        +{photo.tags.length - 2}
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