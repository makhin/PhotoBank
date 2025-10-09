import type { RefObject, CSSProperties } from 'react';
import { Maximize2 } from 'lucide-react';
import type { FaceBoxDto } from '@photobank/shared/api/photobank';

import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { Button } from '@/shared/ui/button';
import { FaceOverlay } from '@/components/FaceOverlay';

import type { PhotoDetails } from '../types';

interface PhotoViewerProps {
    photo: PhotoDetails;
    containerRef: RefObject<HTMLDivElement | null>;
    showFaceBoxes: boolean;
    calculateFacePosition: (faceBox: FaceBoxDto) => CSSProperties;
    onOpenViewer: () => void;
}

export const PhotoViewer = ({
    photo,
    containerRef,
    showFaceBoxes,
    calculateFacePosition,
    onOpenViewer,
}: PhotoViewerProps) => {
    return (
        <div className="lg:col-span-2 h-full flex flex-col min-h-0">
            <Card className="flex-1 overflow-hidden border-0 lg:border-r lg:rounded-none bg-background">
                <CardHeader className="pb-4 border-b border-border flex items-center justify-between">
                    <div>
                        <CardTitle className="text-2xl font-bold">{photo.name ?? ''}</CardTitle>
                        {photo.captions && photo.captions.length > 0 && (
                            <p className="text-muted-foreground italic">{photo.captions[0]}</p>
                        )}
                    </div>
                    <Button size="icon" variant="ghost" aria-label="Open viewer" onClick={onOpenViewer}>
                        <Maximize2 className="w-4 h-4" />
                    </Button>
                </CardHeader>
                <CardContent className="p-0 flex-1 h-full min-h-0">
                    <div ref={containerRef} className="relative bg-black flex items-center justify-center h-full w-full">
                        <img
                            loading="lazy"
                            src={photo.previewUrl ?? undefined}
                            alt={photo.name ?? ''}
                            className="max-h-full max-w-full object-contain"
                        />
                        {showFaceBoxes &&
                            photo.faces?.map((face, index) => {
                                if (!face.faceBox) {
                                    return null;
                                }

                                const facePosition = calculateFacePosition(face.faceBox);

                                return <FaceOverlay key={face.id ?? index} face={face} index={index} style={facePosition} />;
                            })}
                    </div>
                </CardContent>
            </Card>
        </div>
    );
};
