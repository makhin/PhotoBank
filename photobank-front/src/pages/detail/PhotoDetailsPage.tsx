import {useState, useRef, useEffect} from 'react';
import {useParams} from "react-router-dom";

import {Card, CardContent, CardHeader, CardTitle} from '@/components/ui/card';
import {Badge} from '@/components/ui/badge';
import {Popover, PopoverContent, PopoverTrigger} from '@/components/ui/popover';
import {Label} from '@/components/ui/label';
import {Input} from '@/components/ui/input';
import {Textarea} from '@/components/ui/textarea';
import {ScrollArea} from '@/components/ui/scroll-area';
import type {FaceBoxDto} from '@/entities/photo/model.ts';
import {useGetPhotoByIdQuery} from "@/entities/photo/api.ts";

const PhotoDetailsPage = () => {
    const [imageLoaded, setImageLoaded] = useState(false);
    const [imageNaturalSize, setImageNaturalSize] = useState({width: 0, height: 0});
    const [imageDisplaySize, setImageDisplaySize] = useState({width: 0, height: 0});
    const [containerSize, setContainerSize] = useState({width: 0, height: 0});
    const imageRef = useRef<HTMLImageElement>(null);
    const containerRef = useRef<HTMLDivElement>(null);

    const {id} = useParams<{ id: string }>();
    const photoId = id ? +id : 0;
    const {data: photo, error} = useGetPhotoByIdQuery(photoId);

    useEffect(() => {
        if (error) {
            console.error("Ошибка загрузки фото:", error);
        }
    }, [error]);

    const calculateImageSize = (naturalWidth: number, naturalHeight: number, containerWidth: number, containerHeight: number) => {
        // Rule 1: If image is smaller than container, show as is
        if (naturalWidth <= containerWidth && naturalHeight <= containerHeight) {
            return {width: naturalWidth, height: naturalHeight};
        }

        // Calculate scale factors
        const scaleByWidth = containerWidth / naturalWidth;
        const scaleByHeight = containerHeight / naturalHeight;

        // Rule 2 & 3: Scale by the dimension that requires more scaling (smaller scale factor)
        // This ensures the image fits within the container
        const scale = Math.min(scaleByWidth, scaleByHeight);

        return {
            width: naturalWidth * scale,
            height: naturalHeight * scale
        };
    };

    const updateSizes = () => {
        console.log("Updating sizes...", imageLoaded);
        if (containerRef.current && imageRef.current && imageLoaded) {
            const containerRect = containerRef.current.getBoundingClientRect();
            const newContainerSize = {
                width: containerRect.width,
                height: containerRect.height
            };

            setContainerSize(newContainerSize);

            const calculatedSize = calculateImageSize(
                imageNaturalSize.width,
                imageNaturalSize.height,
                newContainerSize.width,
                newContainerSize.height
            );

            setImageDisplaySize(calculatedSize);
        }
    };

    useEffect(() => {
        const resizeObserver = new ResizeObserver(updateSizes);
        if (containerRef.current) {
            resizeObserver.observe(containerRef.current);
        }

        window.addEventListener('resize', updateSizes);

        return () => {
            resizeObserver.disconnect();
            window.removeEventListener('resize', updateSizes);
        };
    }, [imageLoaded, imageNaturalSize]);

    // Add effect to handle initial sizing when photo changes
    useEffect(() => {
        if (photo && photo.previewImage) {
            setImageLoaded(false);
            setImageNaturalSize({width: 0, height: 0});
            setImageDisplaySize({width: 0, height: 0});
        }
    }, [photo]);

    const handleImageLoad = () => {
        if (imageRef.current) {
            const naturalSize = {
                width: imageRef.current.naturalWidth,
                height: imageRef.current.naturalHeight
            };
            setImageNaturalSize(naturalSize);
            setImageLoaded(true);

            // Trigger size calculation immediately after image loads
            setTimeout(updateSizes, 0);
        }
    };

    const calculateFacePosition = (faceBox: FaceBoxDto) => {
        if (!imageLoaded || !imageDisplaySize.width || !imageDisplaySize.height) {
            return {display: 'none'};
        }

        const scaleX = imageDisplaySize.width / imageNaturalSize.width;
        const scaleY = imageDisplaySize.height / imageNaturalSize.height;

        const left = faceBox.left * scaleX;
        const top = faceBox.top * scaleY;
        const width = faceBox.width * scaleX;
        const height = faceBox.height * scaleY;

        return {
            position: 'absolute' as const,
            left: `${left.toString()}px`,
            top: `${top.toString()}px`,
            width: `${width.toString()}px`,
            height: `${height.toString()}px`,
            border: '2px solid #3b82f6',
            borderRadius: '4px',
            backgroundColor: 'rgba(59, 130, 246, 0.1)',
            cursor: 'pointer',
            transition: 'all 0.2s ease-in-out',
            zIndex: 10,
        };
    };

    const formatDate = (dateString?: string) => {
        if (!dateString) return 'Not specified';
        return new Date(dateString).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    const getGenderText = (gender?: boolean) => {
        if (gender === undefined) return 'Unknown';
        return gender ? 'Female' : 'Male';
    };

    if (!photo) {
        return <div className="p-4">Загрузка...</div>;
    }
    return (
        <div className="h-screen w-screen bg-background text-foreground overflow-hidden">
            <div className="h-full w-full grid grid-cols-1 lg:grid-cols-3 gap-0">
                {/* Main Photo Display */}
                <div className="lg:col-span-2 h-full flex flex-col">
                    <Card className="flex-1 overflow-hidden border-0 lg:border-r lg:rounded-none bg-background">
                        <CardHeader className="pb-4 border-b border-border">
                            <CardTitle className="text-2xl font-bold">
                                {photo.name}
                            </CardTitle>
                            {photo.captions && photo.captions.length > 0 && (
                                <p className="text-muted-foreground italic">{photo.captions[0]}</p>
                            )}
                        </CardHeader>
                        <CardContent className="p-0 flex-1">
                            <div
                                ref={containerRef}
                                className="relative bg-black flex items-center justify-center h-full w-full"
                            >
                                <img
                                    ref={imageRef}
                                    src={`data:image/jpeg;base64,${photo.previewImage}`}
                                    alt={photo.name}
                                    onLoad={handleImageLoad}
                                    style={{
                                        width: imageDisplaySize.width || 'auto',
                                        height: imageDisplaySize.height || 'auto',
                                        maxWidth: '100%',
                                        maxHeight: '100%'
                                    }}
                                    className="block"
                                />

                                {/* Face Detection Overlays */}
                                {photo.faces && photo.faces.map((face, index) => (
                                    <Popover key={face.id || index}>
                                        <PopoverTrigger asChild>
                                            <div
                                                style={calculateFacePosition(face.faceBox)}
                                                className="hover:border-blue-400 hover:bg-blue-200/20"
                                            />
                                        </PopoverTrigger>
                                        <PopoverContent className="w-80 bg-popover border-border shadow-xl">
                                            <div className="space-y-3">
                                                <h4 className="font-semibold border-b border-border pb-2">
                                                    Face Details
                                                </h4>
                                                <div className="grid grid-cols-2 gap-3 text-sm">
                                                    <div>
                                                        <Label className="text-muted-foreground">Age</Label>
                                                        <p className="font-medium">{face.age || 'Unknown'}</p>
                                                    </div>
                                                    <div>
                                                        <Label className="text-muted-foreground">Gender</Label>
                                                        <p className="font-medium">{getGenderText(face.gender)}</p>
                                                    </div>
                                                    {face.personId && (
                                                        <div className="col-span-2">
                                                            <Label className="text-muted-foreground">Person ID</Label>
                                                            <p className="font-medium">{face.personId}</p>
                                                        </div>
                                                    )}
                                                </div>
                                                {face.friendlyFaceAttributes && (
                                                    <div>
                                                        <Label className="text-muted-foreground">Attributes</Label>
                                                        <p className="text-sm mt-1">
                                                            {face.friendlyFaceAttributes}
                                                        </p>
                                                    </div>
                                                )}
                                            </div>
                                        </PopoverContent>
                                    </Popover>
                                ))}
                            </div>
                        </CardContent>
                    </Card>
                </div>

                {/* Photo Properties Panel */}
                <div className="h-full overflow-y-auto bg-background border-l border-border">
                    <ScrollArea className="h-full">
                        <div className="p-4 space-y-4">
                            <Card className="bg-card border-border">
                                <CardHeader className="pb-3">
                                    <CardTitle className="text-lg">Photo Properties</CardTitle>
                                </CardHeader>
                                <CardContent className="space-y-3">
                                    <div>
                                        <Label className="text-muted-foreground text-xs">Name</Label>
                                        <Input value={photo.name} readOnly className="mt-1 bg-muted"/>
                                    </div>

                                    {photo.id && (
                                        <div>
                                            <Label className="text-muted-foreground text-xs">ID</Label>
                                            <Input value={photo.id.toString()} readOnly className="mt-1 bg-muted"/>
                                        </div>
                                    )}

                                    <div>
                                        <Label className="text-muted-foreground text-xs">Taken Date</Label>
                                        <Input value={formatDate(photo.takenDate)} readOnly className="mt-1 bg-muted"/>
                                    </div>

                                    {photo.width && photo.height && (
                                        <div className="grid grid-cols-2 gap-2">
                                            <div>
                                                <Label className="text-muted-foreground text-xs">Width</Label>
                                                <Input value={`${photo.width.toString()}px`} readOnly
                                                       className="mt-1 bg-muted"/>
                                            </div>
                                            <div>
                                                <Label className="text-muted-foreground text-xs">Height</Label>
                                                <Input value={`${photo.height.toString()}px`} readOnly
                                                       className="mt-1 bg-muted"/>
                                            </div>
                                        </div>
                                    )}

                                    {photo.scale && (
                                        <div>
                                            <Label className="text-muted-foreground text-xs">Scale</Label>
                                            <Input value={photo.scale.toString()} readOnly className="mt-1 bg-muted"/>
                                        </div>
                                    )}

                                    {photo.orientation && (
                                        <div>
                                            <Label className="text-muted-foreground text-xs">Orientation</Label>
                                            <Input value={photo.orientation.toString()} readOnly
                                                   className="mt-1 bg-muted"/>
                                        </div>
                                    )}
                                </CardContent>
                            </Card>

                            {/* Tags Section */}
                            {photo.tags && photo.tags.length > 0 && (
                                <Card className="bg-card border-border">
                                    <CardHeader className="pb-3">
                                        <CardTitle className="text-lg">Tags</CardTitle>
                                    </CardHeader>
                                    <CardContent>
                                        <div className="flex flex-wrap gap-2">
                                            {photo.tags.map((tag, index) => (
                                                <Badge key={index} variant="secondary"
                                                       className="bg-secondary text-secondary-foreground">
                                                    {tag}
                                                </Badge>
                                            ))}
                                        </div>
                                    </CardContent>
                                </Card>
                            )}

                            {/* Captions Section */}
                            {photo.captions && photo.captions.length > 0 && (
                                <Card className="bg-card border-border">
                                    <CardHeader className="pb-3">
                                        <CardTitle className="text-lg">Captions</CardTitle>
                                    </CardHeader>
                                    <CardContent>
                                        <div className="space-y-2">
                                            {photo.captions.map((caption, index) => (
                                                <Textarea
                                                    key={index}
                                                    value={caption}
                                                    readOnly
                                                    className="min-h-[60px] resize-none bg-muted"
                                                />
                                            ))}
                                        </div>
                                    </CardContent>
                                </Card>
                            )}

                            {/* Content Analysis Scores */}
                            {(photo.adultScore !== undefined || photo.racyScore !== undefined) && (
                                <Card className="bg-card border-border">
                                    <CardHeader className="pb-3">
                                        <CardTitle className="text-lg">Content Analysis</CardTitle>
                                    </CardHeader>
                                    <CardContent className="space-y-3">
                                        {photo.adultScore !== undefined && (
                                            <div>
                                                <Label className="text-muted-foreground text-xs">Adult Score</Label>
                                                <div className="flex items-center gap-2 mt-1">
                                                    <div className="flex-1 bg-muted rounded-full h-2">
                                                        <div
                                                            className="bg-orange-500 h-2 rounded-full transition-all duration-500"
                                                            style={{width: `${(photo.adultScore * 100).toString()}%`}}
                                                        />
                                                    </div>
                                                    <span className="text-sm font-medium">
                          {(photo.adultScore * 100).toFixed(1)}%
                        </span>
                                                </div>
                                            </div>
                                        )}
                                        {photo.racyScore !== undefined && (
                                            <div>
                                                <Label className="text-muted-foreground text-xs">Racy Score</Label>
                                                <div className="flex items-center gap-2 mt-1">
                                                    <div className="flex-1 bg-muted rounded-full h-2">
                                                        <div
                                                            className="bg-red-500 h-2 rounded-full transition-all duration-500"
                                                            style={{width: `${(photo.racyScore * 100).toString()}%`}}
                                                        />
                                                    </div>
                                                    <span className="text-sm font-medium">
                          {(photo.racyScore * 100).toFixed(1)}%
                        </span>
                                                </div>
                                            </div>
                                        )}
                                    </CardContent>
                                </Card>
                            )}

                            {/* Faces Summary */}
                            {photo.faces && photo.faces.length > 0 && (
                                <Card className="bg-card border-border">
                                    <CardHeader className="pb-3">
                                        <CardTitle className="text-lg">
                                            Detected Faces ({photo.faces.length})
                                        </CardTitle>
                                    </CardHeader>
                                    <CardContent>
                                        <p className="text-sm text-muted-foreground mb-3">
                                            Hover over the blue boxes on the image to see face details.
                                        </p>
                                        <div className="space-y-2">
                                            {photo.faces.map((face, index) => (
                                                <div key={face.id || index} className="p-2 bg-muted rounded text-sm">
                                                    <span className="font-medium">Face {index + 1}:</span>
                                                    <span className="ml-2 text-muted-foreground">
                          {face.age ? `Age ${face.age.toString()}` : 'Age unknown'}
                                                        {face.gender !== undefined && `, ${getGenderText(face.gender)}`}
                        </span>
                                                </div>
                                            ))}
                                        </div>
                                    </CardContent>
                                </Card>
                            )}
                        </div>
                    </ScrollArea>
                </div>
            </div>
        </div>
    );
};

export default PhotoDetailsPage;
