import {
    usePhotosGetPhoto,
    getPhotosGetPhotoQueryKey,
    type photosGetPhotoResponse200,
} from '@photobank/shared/api/photobank';
import { useTranslation } from 'react-i18next';

import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/shared/ui/dialog';

interface PhotoPreviewModalProps {
    photoId: number | null;
    onOpenChange: (open: boolean) => void;
}

const PhotoPreviewModal = ({ photoId, onOpenChange }: PhotoPreviewModalProps) => {
    const { data: photo, isFetching } = usePhotosGetPhoto<photosGetPhotoResponse200['data']>(photoId ?? 0, {
        query: {
            enabled: photoId !== null,
            queryKey: getPhotosGetPhotoQueryKey(photoId ?? 0),
            select: (resp) => (resp as photosGetPhotoResponse200).data,
        },
    });
    const { t } = useTranslation();

    return (
        <Dialog open={photoId !== null} onOpenChange={onOpenChange}>
            <DialogContent className="max-w-none w-screen h-screen top-0 left-0 translate-x-0 translate-y-0">
                <DialogHeader>
                    <DialogTitle>{photo?.name || t('previewModalFallbackTitle')}</DialogTitle>
                </DialogHeader>
                {isFetching || !photo ? (
                    <p className="p-4">{t('loadingText')}</p>
                ) : (
                    <img
                        src={`data:image/jpeg;base64,${photo.previewImage}`}
                        alt={photo.name}
                        className="max-h-full max-w-full w-auto h-auto mx-auto object-contain"
                    />
                )}
            </DialogContent>
        </Dialog>
    );
};

export default PhotoPreviewModal;
