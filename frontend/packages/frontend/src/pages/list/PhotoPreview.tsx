import { useState } from 'react';
import { Image } from 'lucide-react';

interface PhotoPreviewProps {
    thumbnail: string;
    alt: string;
    className?: string;
}

const PhotoPreview = ({ thumbnail, alt, className = "" }: PhotoPreviewProps) => {
    const [imageError, setImageError] = useState(false);

    return (
        <div className={`w-[50px] h-[50px] rounded-lg border border-border overflow-hidden bg-muted flex-shrink-0 ${className}`}>
            {imageError ? (
                <div className="flex items-center justify-center w-full h-full">
                    <Image className="w-5 h-5 text-muted-foreground" />
                </div>
            ) : (
                <img
                    src={`data:image/jpeg;base64,${thumbnail}`}
                    alt={alt}
                    className="w-full h-full object-cover transition-transform hover:scale-110 duration-200"
                    onError={() => { setImageError(true); }}
                />
            )}
        </div>
    );
};

export default PhotoPreview;