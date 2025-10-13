import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useIsAdmin } from '@photobank/shared';
import { formatDateTime } from '@photobank/shared/format';
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
import { useImageContainerSizing } from '@/hooks/useImageContainerSizing';
import { usePhotoGeodata } from '@/hooks/usePhotoGeodata';


interface PhotoDetailsPageProps {
    photoId?: number;
    onClose?: () => void;
}

const PhotoDetailsPage = ({ photoId: propPhotoId, onClose }: PhotoDetailsPageProps) => {
    const [showFaceBoxes, setShowFaceBoxes] = useState(false);
    const [imageMeasuredSize, setImageMeasuredSize] = useState<{ width: number; height: number } | null>(null);
    const persons = useAppSelector((state) => state.metadata.persons);
    const isAdmin = useIsAdmin() ?? false;
    const dispatch = useAppDispatch();
    const { mutateAsync: updateFace } = useFacesUpdate();
    const { t } = useTranslation();

    const containerRef = useRef<HTMLDivElement | null>(null);

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
    const { placeName, hasValidLocation } = usePhotoGeodata(location);

    const formattedTakenDate = useMemo(
        () => (photoData?.takenDate ? formatDateTime(photoData.takenDate) : ''),
        [photoData?.takenDate],
    );

    const imageNaturalSize = useMemo(() => {
        if (photoData?.width && photoData.height && photoData.scale) {
            return {width: photoData.width * photoData.scale, height: photoData.height * photoData.scale};
        }
        return {width: 0, height: 0};
    }, [photoData]);
    const { containerSize, imageDisplaySize } = useImageContainerSizing({
        containerRef,
        imageNaturalSize,
        imageMeasuredSize,
    });

    const handleImageLoad = useCallback((size: { width: number; height: number }) => {
        setImageMeasuredSize(size);
    }, []);

    useEffect(() => {
        setImageMeasuredSize(null);
    }, [photoData?.id]);

    useEffect(() => {
        if (error) {
            logger.error('Failed to load photo:', error);
        }
    }, [error]);

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
        <div className="w-full min-h-dvh bg-background text-foreground overflow-hidden">
            <div className="grid h-dvh w-full grid-cols-1 grid-rows-[minmax(0,2fr)_minmax(0,1fr)] overflow-hidden lg:grid-cols-3 lg:grid-rows-1">
                <PhotoViewer
                    photo={photoData}
                    containerRef={containerRef}
                    showFaceBoxes={showFaceBoxes}
                    calculateFacePosition={calculateFacePosition}
                    onOpenViewer={handleOpenViewer}
                    onClose={onClose}
                    onImageLoad={handleImageLoad}
                />
                <div className="flex h-full min-h-0 flex-col bg-background border-t border-border lg:border-l lg:border-t-0">
                    <ScrollArea className="flex-1 min-h-0">
                        <div className="h-full p-4 space-y-4">
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
