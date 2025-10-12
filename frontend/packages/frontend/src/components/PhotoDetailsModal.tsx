import { VisuallyHidden } from '@radix-ui/react-visually-hidden';

import PhotoDetailsPage from '@/pages/detail/PhotoDetailsPage';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/shared/ui/dialog';

interface PhotoDetailsModalProps {
    photoId: number | null;
    onOpenChange: (open: boolean) => void;
}

const PhotoDetailsModal = ({ photoId, onOpenChange }: PhotoDetailsModalProps) => {
    const handleClose = () => {
        onOpenChange(false);
    };

    return (
        <Dialog open={photoId !== null} onOpenChange={onOpenChange}>
            <DialogContent
                className="!max-w-none sm:!max-w-none w-screen h-screen top-0 left-0 translate-x-0 translate-y-0 p-0"
                showCloseButton={false}
            >
                <DialogHeader>
                    <VisuallyHidden>
                        <DialogTitle>Photo details</DialogTitle>
                    </VisuallyHidden>
                </DialogHeader>
                {photoId !== null && (
                    <div className="h-full w-full">
                        <PhotoDetailsPage photoId={photoId} onClose={handleClose} />
                    </div>
                )}
            </DialogContent>
        </Dialog>
    );
};

export default PhotoDetailsModal;
