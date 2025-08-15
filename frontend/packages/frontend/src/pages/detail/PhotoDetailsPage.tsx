import { useEffect, useMemo, useRef, useState, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import { formatDate, getOrientation, getPlaceByGeoPoint, useIsAdmin } from '@photobank/shared';
import { logger } from '@photobank/shared/utils/logger';
import type { FaceBoxDto } from '@photobank/shared/api/photobank';
import {
    photoPropertiesTitle,
    nameLabel,
    idLabel,
    takenDateLabel,
    widthLabel,
    heightLabel,
    scaleLabel,
    orientationLabel,
    locationLabel,
    openInMapsText,
    tagsTitle,
    captionsTitle,
    contentAnalysisTitle,
    adultScoreLabel,
    racyScoreLabel,
    detectedFacesTitle,
    showFaceBoxesLabel,
    hoverFaceHint,
} from '@photobank/shared/constants';

import { useGetPhotoByIdQuery, useUpdateFaceMutation } from '@/shared/api.ts';
import { useAppSelector } from '@/app/hook.ts';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { Badge } from '@/shared/ui/badge';
import { Label } from '@/shared/ui/label';
import { Input } from '@/shared/ui/input';
import { Textarea } from '@/shared/ui/textarea';
import { ScrollArea } from '@/shared/ui/scroll-area';
import { Checkbox } from '@/shared/ui/checkbox';
import { ScoreBar } from '@/components/ScoreBar';
import { FaceOverlay } from '@/components/FaceOverlay.tsx';
import { FacePersonSelector } from '@/components/FacePersonSelector.tsx';
import { useViewer } from '@/features/viewer/state';
import { pushPhotoId } from '@/features/viewer/urlSync';
import { Button } from '@/shared/ui/button';
import { Maximize2 } from 'lucide-react';


const calculateImageSize = (naturalWidth: number, naturalHeight: number, containerWidth: number, containerHeight: number) => {
    if (naturalWidth <= containerWidth && naturalHeight <= containerHeight) {
        return {width: naturalWidth, height: naturalHeight, scale: 1};
    }

    const scaleByWidth = containerWidth / naturalWidth;
    const scaleByHeight = containerHeight / naturalHeight;

    const scale = Math.min(scaleByWidth, scaleByHeight);

    return {
        width: naturalWidth * scale,
        height: naturalHeight * scale,
        scale: scale
    };
};

interface PhotoDetailsPageProps {
    photoId?: number;
}

const PhotoDetailsPage = ({ photoId: propPhotoId }: PhotoDetailsPageProps) => {
    const [imageDisplaySize, setImageDisplaySize] = useState({width: 0, height: 0, scale: 1});
    const [containerSize, setContainerSize] = useState({width: 0, height: 0});
    const [showFaceBoxes, setShowFaceBoxes] = useState(false);
    const [placeName, setPlaceName] = useState('');
    const persons = useAppSelector((state) => state.metadata.persons);
    const isAdmin = useIsAdmin();
    const [updateFace] = useUpdateFaceMutation();

    const containerRef = useRef<HTMLDivElement>(null);

    const {id} = useParams<{ id: string }>();
    const photoId = propPhotoId ?? (id ? +id : 0);
    const {data: photo, error} = useGetPhotoByIdQuery(photoId, { skip: photoId === 0 });

    const formattedTakenDate = useMemo(() =>
        photo?.takenDate ? formatDate(photo.takenDate) : '',
    [photo?.takenDate]);

    const previewImageSrc = photo?.previewImage && `data:image/jpeg;base64,${photo.previewImage}`;

    const imageNaturalSize = useMemo(() => {
        if (photo?.width && photo.height && photo.scale) {
            return {width: photo.width * photo.scale, height: photo.height * photo.scale};
        }
        return {width: 0, height: 0};
    }, [photo]);

    const updateSizes = useCallback(() => {
        if (containerRef.current) {
            const containerRect = containerRef.current.getBoundingClientRect();
            const newContainerSize = {
                width: containerRect.width,
                height: containerRect.height
            };

            if (newContainerSize.width !== containerSize.width || newContainerSize.height !== containerSize.height) {
                setContainerSize(newContainerSize);
            }

            const calculatedSize = calculateImageSize(
                imageNaturalSize.width,
                imageNaturalSize.height,
                newContainerSize.width,
                newContainerSize.height
            );

            if (calculatedSize.width !== imageDisplaySize.width || calculatedSize.height !== imageDisplaySize.height) {
                setImageDisplaySize(calculatedSize);
            }
        }
    }, [imageNaturalSize, containerSize, imageDisplaySize]);

    useEffect(() => {
        const resizeObserver = new ResizeObserver(updateSizes);
        const container = containerRef.current;
        if (container) resizeObserver.observe(container);

        window.addEventListener('resize', updateSizes);
        updateSizes();

        return () => {
            resizeObserver.disconnect();
            window.removeEventListener('resize', updateSizes);
        };
    }, [updateSizes]);

    useEffect(() => {
        if (error) {
            logger.error('Ошибка загрузки фото:', error);
        }
    }, [error]);

    useEffect(() => {
        if (!photo?.location) {
            setPlaceName('');
            return;
        }
        const controller = new AbortController();
        (async () => {
            const name = await getPlaceByGeoPoint(photo.location);
            if (!controller.signal.aborted) setPlaceName(name);
        })();
        return () => { controller.abort(); };
    }, [photo?.location]);

    const calculateFacePosition = (faceBox: FaceBoxDto) => {
        if (!imageDisplaySize.width || !imageDisplaySize.height) {
            return {display: 'none'};
        }

        const scale = imageDisplaySize.scale
        const offsetLeft = (containerSize.width - imageDisplaySize.width) / 2;
        const offsetTop = (containerSize.height - imageDisplaySize.height) / 2;

        const left = faceBox.left * scale + offsetLeft;
        const top = faceBox.top * scale + offsetTop;
        const width = faceBox.width * scale;
        const height = faceBox.height * scale;

        return {
            left: `${left.toString()}px`,
            top: `${top.toString()}px`,
            width: `${width.toString()}px`,
            height: `${height.toString()}px`,
        };
    };

    if (!photo) {
        return <div className="p-4">Загрузка...</div>;
    }

    return (
        <div className="h-screen w-screen bg-background text-foreground overflow-hidden">
            <div className="h-full w-full grid grid-cols-1 lg:grid-cols-3 gap-0">
                {/* Main Photo Display */}
                <div className="lg:col-span-2 h-full flex flex-col min-h-0">
                    <Card className="flex-1 overflow-hidden border-0 lg:border-r lg:rounded-none bg-background">
                        <CardHeader className="pb-4 border-b border-border flex items-center justify-between">
                            <div>
                                <CardTitle className="text-2xl font-bold">
                                    {photo.name}
                                </CardTitle>
                                {photo.captions && photo.captions.length > 0 && (
                                    <p className="text-muted-foreground italic">{photo.captions[0]}</p>
                                )}
                            </div>
                            <Button
                                size="icon"
                                variant="ghost"
                                aria-label="Open viewer"
                                onClick={() => {
                                    if (previewImageSrc) {
                                        useViewer.getState().open([
                                            {
                                                id: photo.id,
                                                src: previewImageSrc,
                                                thumb: previewImageSrc,
                                                title: photo.name,
                                            },
                                        ], 0);
                                        pushPhotoId(photo.id);
                                    }
                                }}
                            >
                                <Maximize2 className="w-4 h-4" />
                            </Button>
                        </CardHeader>
                        <CardContent className="p-0 flex-1 h-full min-h-0">
                            <div
                                ref={containerRef}
                                className="relative bg-black flex items-center justify-center h-full w-full"
                            >
                                <img
                                    loading="lazy"
                                    src={previewImageSrc}
                                    alt={photo.name}
                                    className="max-h-full max-w-full object-contain"
                                />
                                {showFaceBoxes &&
                                    photo.faces?.map((face, index) => (
                                        <FaceOverlay
                                            key={face.id}
                                            face={face}
                                            index={index}
                                            style={calculateFacePosition(face.faceBox)}
                                        />
                                    ))}
                            </div>
                        </CardContent>
                    </Card>
                </div>

                {/* Photo Properties Panel */}
                <div className="h-full overflow-y-auto bg-background border-l border-border">
                    <ScrollArea className="h-full">
                        <div className="p-4 space-y-4">
                            <Card className="bg-card border-border">
                                <CardHeader className="pb-3">
                                    <CardTitle className="text-lg">{photoPropertiesTitle}</CardTitle>
                                </CardHeader>
                                <CardContent className="space-y-3">
                                    <div>
                                        <Label className="text-muted-foreground text-xs">{nameLabel}</Label>
                                        <Input value={photo.name} readOnly className="mt-1 bg-muted"/>
                                    </div>

                                    {photo.id && (
                                        <div>
                                            <Label className="text-muted-foreground text-xs">{idLabel}</Label>
                                            <Input value={photo.id.toString()} readOnly className="mt-1 bg-muted"/>
                                        </div>
                                    )}

                                    <div>
                                        <Label className="text-muted-foreground text-xs">{takenDateLabel}</Label>
                                        <Input value={formattedTakenDate} readOnly className="mt-1 bg-muted"/>
                                    </div>

                                    {photo.width && photo.height && (
                                        <div className="grid grid-cols-2 gap-2">
                                            <div>
                                                <Label className="text-muted-foreground text-xs">{widthLabel}</Label>
                                                <Input value={`${photo.width.toString()}px`} readOnly
                                                       className="mt-1 bg-muted"/>
                                            </div>
                                            <div>
                                                <Label className="text-muted-foreground text-xs">{heightLabel}</Label>
                                                <Input value={`${photo.height.toString()}px`} readOnly
                                                       className="mt-1 bg-muted"/>
                                            </div>
                                        </div>
                                    )}

                                    {photo.scale && (
                                        <div>
                                            <Label className="text-muted-foreground text-xs">{scaleLabel}</Label>
                                            <Input value={photo.scale.toString()} readOnly className="mt-1 bg-muted"/>
                                        </div>
                                    )}

                                    {photo.orientation && (
                                        <div>
                                            <Label className="text-muted-foreground text-xs">{orientationLabel}</Label>
                                            <Input value={getOrientation(photo.orientation ?? undefined)} readOnly
                                                   className="mt-1 bg-muted"/>
                                        </div>
                                    )}

                                    {photo.location && placeName && (
                                        <div>
                                            <Label className="text-muted-foreground text-xs">{locationLabel}</Label>
                                            <a
                                                href={`https://www.google.com/maps?q=${photo.location.latitude},${photo.location.longitude}`}
                                                target="_blank"
                                                rel="noopener noreferrer"
                                                className="mt-1 block text-primary underline"
                                            >
                                                {openInMapsText}: {placeName}
                                            </a>
                                        </div>
                                    )}
                                </CardContent>
                            </Card>

                            {/* Tags Section */}
                            {photo.tags && photo.tags.length > 0 && (
                                <Card className="bg-card border-border">
                                    <CardHeader className="pb-3">
                                        <CardTitle className="text-lg">{tagsTitle}</CardTitle>
                                    </CardHeader>
                                    <CardContent>
                                        <div className="flex flex-wrap gap-2">
                                            {photo.tags.map((tag) => (
                                                <Badge key={tag} variant="secondary"
                                                       className="bg-secondary text-secondary-foreground">
                                                    {tag}
                                                </Badge>
                                            ))}
                                        </div>
                                    </CardContent>
                                </Card>
                            )}

                            {/* Captions Section */}
                            {photo.captions && photo.captions.length > 0 && (
                                <Card className="bg-card border-border">
                                    <CardHeader className="pb-3">
                                        <CardTitle className="text-lg">{captionsTitle}</CardTitle>
                                    </CardHeader>
                                    <CardContent>
                                        <div className="space-y-2">
                                            {photo.captions.map((caption) => (
                                                <Textarea
                                                    key={caption}
                                                    value={caption}
                                                    readOnly
                                                    className="min-h-[60px] resize-none bg-muted"
                                                />
                                            ))}
                                        </div>
                                    </CardContent>
                                </Card>
                            )}

                            <Card className="bg-card border-border">
                                <CardHeader className="pb-3">
                                    <CardTitle className="text-lg">{contentAnalysisTitle}</CardTitle>
                                </CardHeader>
                                <CardContent className="space-y-3">
                                    <ScoreBar label={adultScoreLabel} score={photo.adultScore}
                                              colorClass="bg-orange-500"/>
                                    <ScoreBar label={racyScoreLabel} score={photo.racyScore}
                                              colorClass="bg-red-500"/>
                                </CardContent>
                            </Card>

                            {/* Faces Summary */}
                            {photo.faces && photo.faces.length > 0 && (
                                <Card className="bg-card border-border">
                                <CardHeader className="pb-3">
                                    <div className="flex items-center justify-between">
                                        <CardTitle className="text-lg">
                                            {detectedFacesTitle} ({photo.faces.length})
                                        </CardTitle>
                                        <div className="flex items-center space-x-2">
                                            <Checkbox
                                                id="show-face-boxes"
                                                checked={showFaceBoxes}
                                                onCheckedChange={(v) => setShowFaceBoxes(!!v)}
                                            />
                                            <Label htmlFor="show-face-boxes" className="text-sm">
                                                {showFaceBoxesLabel}
                                            </Label>
                                        </div>
                                    </div>
                                </CardHeader>
                                    <CardContent>
                                        <p className="text-sm text-muted-foreground mb-3">
                                            {hoverFaceHint}
                                        </p>
                                        <div className="space-y-2">
                                            {photo.faces.map((face, index) => {
                                                return (
                                                    <FacePersonSelector
                                                        key={face.id}
                                                        faceIndex={index}
                                                        personId={face.personId ?? undefined}
                                                        persons={persons}
                                                        disabled={!showFaceBoxes || !isAdmin}
                                                          onChange={(personId) => {
                                                              void updateFace({
                                                                  faceId: face.id,
                                                                  personId: personId ?? -1,
                                                              });
                                                          }}
                                                      />
                                                  );
                                            })}
                                        </div>
                                    </CardContent>
                                </Card>
                            )}
                        </div>
                    </ScrollArea>
                </div>
            </div>
        </div>
    );
};

export default PhotoDetailsPage;
