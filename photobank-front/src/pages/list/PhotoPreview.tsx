import { useState } from 'react';
import { Image } from 'lucide-react';

import { AspectRatio } from '@/components/ui/aspect-ratio';

interface PhotoPreviewProps {
    thumbnail: string;
    alt: string;
    className?: string;
}

const PhotoPreview = ({ thumbnail, alt, className = "" }: PhotoPreviewProps) => {
    const [imageError, setImageError] = useState(false);

    return (
        <AspectRatio ratio={1} className={`overflow-hidden rounded-md border ${className}`}>
            {imageError ? (
                <div className="flex items-center justify-center w-full h-full bg-muted">
                    <Image className="w-6 h-6 text-muted-foreground" />
                </div>
            ) : (
                <img
                    src={`data:image/jpeg;base64,${thumbnail}`}
                    alt={alt}
                    className="object-cover w-full h-full transition-transform hover:scale-105"
                    onError={() => { setImageError(true); }}
                />
            )}
        </AspectRatio>
    );
};

export default PhotoPreview;