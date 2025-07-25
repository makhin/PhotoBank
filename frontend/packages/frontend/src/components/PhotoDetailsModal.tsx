import PhotoDetailsPage from '@/pages/detail/PhotoDetailsPage';
import { Dialog, DialogContent } from '@/components/ui/dialog';

interface PhotoDetailsModalProps {
    photoId: number | null;
    onOpenChange: (open: boolean) => void;
}

const PhotoDetailsModal = ({ photoId, onOpenChange }: PhotoDetailsModalProps) => {
    return (
        <Dialog open={photoId !== null} onOpenChange={onOpenChange}>
            <DialogContent
                className="!max-w-none sm:!max-w-none w-screen h-dvh top-0 left-0 translate-x-0 translate-y-0 p-0"
                showCloseButton={true}
            >
                {photoId !== null && <PhotoDetailsPage photoId={photoId} />}
            </DialogContent>
        </Dialog>
    );
};

export default PhotoDetailsModal;
