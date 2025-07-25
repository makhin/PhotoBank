import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { useGetPhotoByIdQuery } from '@/entities/photo/api';
import { previewModalFallbackTitle, loadingText } from '@photobank/shared/constants';

interface PhotoPreviewModalProps {
    photoId: number | null;
    onOpenChange: (open: boolean) => void;
}

const PhotoPreviewModal = ({ photoId, onOpenChange }: PhotoPreviewModalProps) => {
    const { data: photo, isFetching } = useGetPhotoByIdQuery(photoId ?? 0, { skip: photoId === null });

    return (
        <Dialog open={photoId !== null} onOpenChange={onOpenChange}>
            <DialogContent
                className="!inset-0 !max-w-none sm:!max-w-none w-screen h-dvh translate-x-0 translate-y-0 p-0"
            >
                <DialogHeader>
                    <DialogTitle>{photo?.name || previewModalFallbackTitle}</DialogTitle>
                </DialogHeader>
                {isFetching || !photo ? (
                    <p className="p-4">{loadingText}</p>
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
