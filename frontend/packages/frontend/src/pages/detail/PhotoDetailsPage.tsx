import { useEffect, useMemo, useRef, useState, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import { formatDate, getOrientation, getPlaceByGeoPoint, useIsAdmin } from '@photobank/shared';
import { logger } from '@photobank/shared/utils/logger';
import type { FaceBoxDto, FaceDto } from '@photobank/shared/api/photobank';
import * as PhotosApi from '@photobank/shared/api/photobank';
import { IdentityStatusDto as IdentityStatus, useFacesUpdate } from '@photobank/shared/api/photobank';
import { Maximize2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { useAppDispatch, useAppSelector } from '@/app/hook';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { Badge } from '@/shared/ui/badge';
import { Label } from '@/shared/ui/label';
import { Input } from '@/shared/ui/input';
import { Textarea } from '@/shared/ui/textarea';
import { ScrollArea } from '@/shared/ui/scroll-area';
import { Checkbox } from '@/shared/ui/checkbox';
import { ScoreBar } from '@/components/ScoreBar';
import { FaceOverlay } from '@/components/FaceOverlay';
import { FacePersonSelector } from '@/components/FacePersonSelector';
import { open } from '@/features/viewer/viewerSlice';
import { pushPhotoId } from '@/features/viewer/urlSync';
import { Button } from '@/shared/ui/button';


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
    const imageDisplaySizeRef = useRef(imageDisplaySize);
    const [containerSize, setContainerSize] = useState({width: 0, height: 0});
    const containerSizeRef = useRef(containerSize);
    const [showFaceBoxes, setShowFaceBoxes] = useState(false);
    const [placeName, setPlaceName] = useState('');
    const persons = useAppSelector((state) => state.metadata.persons);
    const isAdmin = useIsAdmin();
    const dispatch = useAppDispatch();
    const { mutateAsync: updateFace } = useFacesUpdate();
    const { t } = useTranslation();

    const containerRef = useRef<HTMLDivElement>(null);

    const {id} = useParams<{ id: string }>();
    const photoId = propPhotoId ?? (id ? +id : 0);
    const { data: photoData, error } = PhotosApi.usePhotosGetPhoto<PhotosApi.photosGetPhotoResponse200['data']>(photoId, {
        query: {
            enabled: photoId !== 0,
            queryKey: PhotosApi.getPhotosGetPhotoQueryKey(photoId),
            select: (resp) => (resp as PhotosApi.photosGetPhotoResponse200).data,
        },
    });

    const location = photoData?.location;

    const hasValidLocation = useMemo(() => {
        if (!location) {
            return false;
        }

        const { latitude, longitude } = location;

        if (latitude == null || longitude == null) {
            return false;
        }

        return !(latitude === 0 && longitude === 0);
    }, [location]);

    const formattedTakenDate = useMemo(
        () => (photoData?.takenDate ? formatDate(photoData.takenDate) : ''),
        [photoData?.takenDate],
    );

    const imageNaturalSize = useMemo(() => {
        if (photoData?.width && photoData.height && photoData.scale) {
            return {width: photoData.width * photoData.scale, height: photoData.height * photoData.scale};
        }
        return {width: 0, height: 0};
    }, [photoData]);
    const imageNaturalSizeRef = useRef(imageNaturalSize);

    const updateSizes = useCallback(() => {
        if (!containerRef.current) return;

        const containerRect = containerRef.current.getBoundingClientRect();
        const newContainerSize = {
            width: containerRect.width,
            height: containerRect.height,
        };

        if (
            newContainerSize.width !== containerSizeRef.current.width ||
            newContainerSize.height !== containerSizeRef.current.height
        ) {
            containerSizeRef.current = newContainerSize;
            setContainerSize(newContainerSize);
        }

        const { width: naturalWidth, height: naturalHeight } = imageNaturalSizeRef.current;
        const calculatedSize = calculateImageSize(
            naturalWidth,
            naturalHeight,
            newContainerSize.width,
            newContainerSize.height,
        );

        if (
            calculatedSize.width !== imageDisplaySizeRef.current.width ||
            calculatedSize.height !== imageDisplaySizeRef.current.height ||
            calculatedSize.scale !== imageDisplaySizeRef.current.scale
        ) {
            imageDisplaySizeRef.current = calculatedSize;
            setImageDisplaySize(calculatedSize);
        }
    }, []);

    useEffect(() => {
        imageNaturalSizeRef.current = imageNaturalSize;
        updateSizes();
    }, [imageNaturalSize, updateSizes]);

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
        if (!location || !hasValidLocation) {
            setPlaceName('');
            return;
        }
        const controller = new AbortController();
        (async () => {
            const name = await getPlaceByGeoPoint(location);
            if (!controller.signal.aborted) setPlaceName(name);
        })();
        return () => {
            controller.abort();
        };
    }, [hasValidLocation, location]);

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

    if (!photoData) {
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
                                    {photoData.name ?? ''}
                                </CardTitle>
                                {photoData.captions && photoData.captions.length > 0 && (
                                    <p className="text-muted-foreground italic">{photoData.captions[0]}</p>
                                )}
                            </div>
                            <Button
                                size="icon"
                                variant="ghost"
                                aria-label="Open viewer"
                                onClick={() => {
                                    if (photoData?.previewUrl) {
                                        dispatch(
                                            open({
                                                items: [
                                                    {
                                                        id: photoData.id,
                                                        preview: photoData.previewUrl,
                                                        title: photoData.name ?? '',
                                                    },
                                                ],
                                                index: 0,
                                            })
                                        );
                                        pushPhotoId(photoData.id);
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
                                    src={photoData.previewUrl ?? undefined}
                                    alt={photoData.name ?? ''}
                                    className="max-h-full max-w-full object-contain"
                                />
                                {showFaceBoxes &&
                                    photoData.faces?.map((face, index) => {
                                        if (!face.faceBox) {
                                            return null;
                                        }

                                        const facePosition = calculateFacePosition(face.faceBox);

                                        return (
                                            <FaceOverlay
                                                key={face.id}
                                                face={face}
                                                index={index}
                                                style={facePosition}
                                            />
                                        );
                                    })}
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
                                    <CardTitle className="text-lg">{t('photoPropertiesTitle')}</CardTitle>
                                </CardHeader>
                                <CardContent className="space-y-3">
                                    <div>
                                          <Label className="text-muted-foreground text-xs">{t('nameLabel')}</Label>
                                          <Input value={photoData.name ?? ''} readOnly className="mt-1 bg-muted"/>
                                    </div>

                                    {photoData.id != null && (
                                        <div>
                                            <Label className="text-muted-foreground text-xs">{t('idLabel')}</Label>
                                            <Input value={photoData.id.toString()} readOnly className="mt-1 bg-muted"/>
                                        </div>
                                    )}

                                    <div>
                                        <Label className="text-muted-foreground text-xs">{t('takenDateLabel')}</Label>
                                        <Input value={formattedTakenDate} readOnly className="mt-1 bg-muted"/>
                                    </div>

                                      {photoData.width != null && photoData.height != null && (
                                        <div className="grid grid-cols-2 gap-2">
                                            <div>
                                                <Label className="text-muted-foreground text-xs">{t('widthLabel')}</Label>
                                                <Input value={`${photoData.width.toString()}px`} readOnly
                                                       className="mt-1 bg-muted"/>
                                            </div>
                                            <div>
                                                <Label className="text-muted-foreground text-xs">{t('heightLabel')}</Label>
                                                <Input value={`${photoData.height.toString()}px`} readOnly
                                                       className="mt-1 bg-muted"/>
                                            </div>
                                        </div>
                                    )}

                                      {photoData.scale != null && (
                                        <div>
                                            <Label className="text-muted-foreground text-xs">{t('scaleLabel')}</Label>
                                            <Input value={photoData.scale.toString()} readOnly className="mt-1 bg-muted"/>
                                        </div>
                                    )}

                                      {photoData.orientation != null && (
                                        <div>
                                            <Label className="text-muted-foreground text-xs">{t('orientationLabel')}</Label>
                                              <Input
                                                  value={getOrientation(photoData.orientation)}
                                                  readOnly
                                                  className="mt-1 bg-muted"
                                              />
                                        </div>
                                    )}

                                      {hasValidLocation && placeName && location && (
                                        <div>
                                            <Label className="text-muted-foreground text-xs">{t('locationLabel')}</Label>
                                              <a
                                                  href={`https://www.google.com/maps?q=${location.latitude},${location.longitude}`}
                                                target="_blank"
                                                rel="noopener noreferrer"
                                                className="mt-1 block text-primary underline"
                                            >
                                                {t('openInMapsText')}: {placeName}
                                            </a>
                                        </div>
                                    )}
                                </CardContent>
                            </Card>

                            {/* Tags Section */}
                            {photoData.tags && photoData.tags.length > 0 && (
                                <Card className="bg-card border-border">
                                    <CardHeader className="pb-3">
                                        <CardTitle className="text-lg">{t('tagsTitle')}</CardTitle>
                                    </CardHeader>
                                    <CardContent>
                                        <div className="flex flex-wrap gap-2">
                                            {photoData.tags.map((tag: string) => (
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
                            {photoData.captions && photoData.captions.length > 0 && (
                                <Card className="bg-card border-border">
                                    <CardHeader className="pb-3">
                                        <CardTitle className="text-lg">{t('captionsTitle')}</CardTitle>
                                    </CardHeader>
                                    <CardContent>
                                        <div className="space-y-2">
                                            {photoData.captions.map((caption: string) => (
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
                                    <CardTitle className="text-lg">{t('contentAnalysisTitle')}</CardTitle>
                                </CardHeader>
                                  <CardContent className="space-y-3">
                                        <ScoreBar
                                            label={t('adultScoreLabel')}
                                            score={photoData.adultScore ?? 0}
                                            colorClass="bg-orange-500"
                                        />
                                        <ScoreBar
                                            label={t('racyScoreLabel')}
                                            score={photoData.racyScore ?? 0}
                                            colorClass="bg-red-500"
                                        />
                                </CardContent>
                            </Card>

                            {/* Faces Summary */}
                                {photoData.faces && photoData.faces.length > 0 && (
                                <Card className="bg-card border-border">
                                <CardHeader className="pb-3">
                                    <div className="flex items-center justify-between">
                                        <CardTitle className="text-lg">
                                              {t('detectedFacesTitle')} ({photoData.faces.length})
                                        </CardTitle>
                                        <div className="flex items-center space-x-2">
                                            <Checkbox
                                                id="show-face-boxes"
                                                checked={showFaceBoxes}
                                                onCheckedChange={(v) => setShowFaceBoxes(!!v)}
                                            />
                                            <Label htmlFor="show-face-boxes" className="text-sm">
                                                {t('showFaceBoxesLabel')}
                                            </Label>
                                        </div>
                                    </div>
                                </CardHeader>
                                    <CardContent>
                                        <p className="text-sm text-muted-foreground mb-3">
                                            {t('hoverFaceHint')}
                                        </p>
                                          <div className="space-y-2">
                                                {photoData.faces.map((face: FaceDto, index: number) => {
                                                if (face.id === undefined) return null;
                                                return (
                                                    <FacePersonSelector
                                                        key={face.id}
                                                        faceIndex={index}
                                                        personId={face.personId ?? undefined}
                                                        persons={persons}
                                                        disabled={!showFaceBoxes || !isAdmin}
                                                      onChange={(personId) => {
                                                          void updateFace({
                                                              data: {
                                                                  id: face.id!,
                                                                  personId: personId ?? null,
                                                                  identityStatus:
                                                                      personId == null
                                                                          ? IdentityStatus.StopProcessing
                                                                          : IdentityStatus.Identified,
                                                              },
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
