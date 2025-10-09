import { VisuallyHidden } from '@radix-ui/react-visually-hidden';
import { XIcon } from 'lucide-react';

import PhotoDetailsPage from '@/pages/detail/PhotoDetailsPage';
import { Dialog, DialogClose, DialogContent, DialogHeader, DialogTitle } from '@/shared/ui/dialog';

interface PhotoDetailsModalProps {
    photoId: number | null;
    onOpenChange: (open: boolean) => void;
}

const PhotoDetailsModal = ({ photoId, onOpenChange }: PhotoDetailsModalProps) => {
    return (
        <Dialog open={photoId !== null} onOpenChange={onOpenChange}>
            <DialogContent
                className="!max-w-none sm:!max-w-none w-screen h-screen top-0 left-0 translate-x-0 translate-y-0 p-0 pt-14"
                showCloseButton={false}
            >
                <DialogClose
                    className="ring-offset-background focus:ring-ring absolute top-4 right-4 z-50 rounded-full bg-black/60 p-2 text-white backdrop-blur transition-opacity hover:bg-black/80 focus:outline-none focus:ring-2 focus:ring-offset-2"
                    aria-label="Close"
                >
                    <XIcon className="h-4 w-4" />
                    <span className="sr-only">Close</span>
                </DialogClose>
                <DialogHeader>
                    <VisuallyHidden>
                        <DialogTitle>Photo details</DialogTitle>
                    </VisuallyHidden>
                </DialogHeader>
                {photoId !== null && (
                    <div className="h-full w-full">
                        <PhotoDetailsPage photoId={photoId} />
                    </div>
                )}
            </DialogContent>
        </Dialog>
    );
};

export default PhotoDetailsModal;
