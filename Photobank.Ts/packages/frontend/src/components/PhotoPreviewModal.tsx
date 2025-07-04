import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { useGetPhotoByIdQuery } from '@/entities/photo/api';

interface PhotoPreviewModalProps {
    photoId: number | null;
    onOpenChange: (open: boolean) => void;
}

const PhotoPreviewModal = ({ photoId, onOpenChange }: PhotoPreviewModalProps) => {
    const { data: photo, isFetching } = useGetPhotoByIdQuery(photoId ?? 0, { skip: photoId === null });

    return (
        <Dialog open={photoId !== null} onOpenChange={onOpenChange}>
            <DialogContent className="max-w-3xl">
                <DialogHeader>
                    <DialogTitle>{photo?.name || 'Preview'}</DialogTitle>
                </DialogHeader>
                {isFetching || !photo ? (
                    <p className="p-4">Loading...</p>
                ) : (
                    <img
                        src={`data:image/jpeg;base64,${photo.previewImage}`}
                        alt={photo.name}
                        className="max-h-[80vh] w-auto mx-auto"
                    />
                )}
            </DialogContent>
        </Dialog>
    );
};

export default PhotoPreviewModal;
