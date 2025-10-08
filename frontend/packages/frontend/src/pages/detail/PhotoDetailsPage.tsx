import { useEffect, useMemo, useRef, useState, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import { getPlaceByGeoPoint, useIsAdmin } from '@photobank/shared';
import { formatDate } from '@photobank/shared/format';
import { logger } from '@photobank/shared/utils/logger';
import type { FaceBoxDto, FaceDto } from '@photobank/shared/api/photobank';
import * as PhotosApi from '@photobank/shared/api/photobank';
import { IdentityStatusDto as IdentityStatus, useFacesUpdate } from '@photobank/shared/api/photobank';
import { useTranslation } from 'react-i18next';

import { useAppDispatch, useAppSelector } from '@/app/hook';
import { ScrollArea } from '@/shared/ui/scroll-area';
import { PhotoFacesPanel } from '@/pages/detail/components/PhotoFacesPanel';
import { PhotoGeodataPanel } from '@/pages/detail/components/PhotoGeodataPanel';
import { PhotoPropertiesPanel } from '@/pages/detail/components/PhotoPropertiesPanel';
import { PhotoViewer } from '@/pages/detail/components/PhotoViewer';
import { open } from '@/features/viewer/viewerSlice';
import { pushPhotoId } from '@/features/viewer/urlSync';


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
        scale,
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
            logger.error('Failed to load photo:', error);
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

        const scale = imageDisplaySize.scale;
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
        return <div className="p-4">{t('loadingText')}</div>;
    }

    const handleOpenViewer = () => {
        if (!photoData.previewUrl) {
            return;
        }

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
            }),
        );
        pushPhotoId(photoData.id);
    };

    const handleShowFaceBoxesChange = (value: boolean) => {
        setShowFaceBoxes(value);
    };

    const handleFacePersonChange = (face: FaceDto, personId: number | undefined) => {
        if (!face.id) {
            return;
        }

        void updateFace({
            data: {
                id: face.id,
                personId: personId ?? null,
                identityStatus: personId == null ? IdentityStatus.StopProcessing : IdentityStatus.Identified,
            },
        });
    };

    return (
        <div className="h-screen w-screen bg-background text-foreground overflow-hidden">
            <div className="h-full w-full grid grid-cols-1 lg:grid-cols-3 gap-0">
                <PhotoViewer
                    photo={photoData}
                    containerRef={containerRef}
                    showFaceBoxes={showFaceBoxes}
                    calculateFacePosition={calculateFacePosition}
                    onOpenViewer={handleOpenViewer}
                />
                <div className="h-full overflow-y-auto bg-background border-l border-border">
                    <ScrollArea className="h-full">
                        <div className="p-4 space-y-4">
                            <PhotoPropertiesPanel photo={photoData} formattedTakenDate={formattedTakenDate} />
                            <PhotoGeodataPanel
                                photo={photoData}
                                placeName={placeName}
                                hasValidLocation={hasValidLocation}
                            />
                            <PhotoFacesPanel
                                faces={photoData.faces}
                                showFaceBoxes={showFaceBoxes}
                                onShowFaceBoxesChange={handleShowFaceBoxesChange}
                                onFacePersonChange={handleFacePersonChange}
                                persons={persons}
                                isAdmin={isAdmin}
                            />
                        </div>
                    </ScrollArea>
                </div>
            </div>
        </div>
    );
};

export default PhotoDetailsPage;
