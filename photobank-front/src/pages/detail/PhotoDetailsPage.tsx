import React, { useState, useRef, useEffect } from 'react';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import type {FaceBoxDto, PhotoDto} from '@/entities/photo/model.ts';

interface PhotoViewerProps {
  photo: PhotoDto;
}

const PhotoDetailsPage: React.FC<PhotoViewerProps> = ({ photo }) => {
  const [imageLoaded, setImageLoaded] = useState(false);
  const [imageNaturalSize, setImageNaturalSize] = useState({ width: 0, height: 0 });
  const [imageDisplaySize, setImageDisplaySize] = useState({ width: 0, height: 0 });
  const imageRef = useRef<HTMLImageElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const updateImageSize = () => {
      if (imageRef.current && containerRef.current) {
        const displayRect = imageRef.current.getBoundingClientRect();
        setImageDisplaySize({
          width: displayRect.width,
          height: displayRect.height
        });
      }
    };

    const resizeObserver = new ResizeObserver(updateImageSize);
    if (containerRef.current) {
      resizeObserver.observe(containerRef.current);
    }

    window.addEventListener('resize', updateImageSize);

    return () => {
      resizeObserver.disconnect();
      window.removeEventListener('resize', updateImageSize);
    };
  }, [imageLoaded]);

  const handleImageLoad = () => {
    if (imageRef.current) {
      setImageNaturalSize({
        width: imageRef.current.naturalWidth,
        height: imageRef.current.naturalHeight
      });
      setImageLoaded(true);
    }
  };

  const calculateFacePosition = (faceBox: FaceBoxDto) => {
    if (!imageLoaded || !imageDisplaySize.width || !imageDisplaySize.height) {
      return { display: 'none' };
    }
/*
    const scaleX = imageDisplaySize.width / imageNaturalSize.width;
    const scaleY = imageDisplaySize.height / imageNaturalSize.height;

    const left = parseFloat(faceBox.left) * scaleX;
    const top = parseFloat(faceBox.top) * scaleY;
    const width = parseFloat(faceBox.width) * scaleX;
    const height = parseFloat(faceBox.height) * scaleY;
*/
    return {
      position: 'absolute' as const,
      left: faceBox.left,
      top: faceBox.top,
      width: faceBox.width,
      height: faceBox.height,
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

  return (
      <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-6">
        <div className="max-w-7xl mx-auto grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Main Photo Display */}
          <div className="lg:col-span-2">
            <Card className="overflow-hidden shadow-lg">
              <CardHeader className="pb-4">
                <CardTitle className="text-2xl font-bold text-slate-800">
                  {photo.name}
                </CardTitle>
                {photo.captions && photo.captions.length > 0 && (
                    <p className="text-slate-600 italic">{photo.captions[0]}</p>
                )}
              </CardHeader>
              <CardContent className="p-0">
                <div
                    ref={containerRef}
                    className="relative bg-black flex items-center justify-center min-h-[400px]"
                >
                  <img
                      ref={imageRef}
                      src={photo.previewImage}
                      alt={photo.name}
                      onLoad={handleImageLoad}
                      className="max-w-full max-h-[70vh] object-contain"
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
                        <PopoverContent className="w-80 bg-white shadow-xl border border-slate-200">
                          <div className="space-y-3">
                            <h4 className="font-semibold text-slate-800 border-b pb-2">
                              Face Details
                            </h4>
                            <div className="grid grid-cols-2 gap-3 text-sm">
                              <div>
                                <Label className="text-slate-600">Age</Label>
                                <p className="font-medium">{face.age || 'Unknown'}</p>
                              </div>
                              <div>
                                <Label className="text-slate-600">Gender</Label>
                                <p className="font-medium">{getGenderText(face.gender)}</p>
                              </div>
                              {face.personId && (
                                  <div className="col-span-2">
                                    <Label className="text-slate-600">Person ID</Label>
                                    <p className="font-medium">{face.personId}</p>
                                  </div>
                              )}
                            </div>
                            {face.friendlyFaceAttributes && (
                                <div>
                                  <Label className="text-slate-600">Attributes</Label>
                                  <p className="text-sm text-slate-700 mt-1">
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
          <div className="space-y-6">
            <Card className="shadow-lg">
              <CardHeader>
                <CardTitle className="text-lg text-slate-800">Photo Properties</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <Label className="text-slate-600">Name</Label>
                  <Input value={photo.name} readOnly className="mt-1" />
                </div>

                {photo.id && (
                    <div>
                      <Label className="text-slate-600">ID</Label>
                      <Input value={photo.id.toString()} readOnly className="mt-1" />
                    </div>
                )}

                <div>
                  <Label className="text-slate-600">Taken Date</Label>
                  <Input value={formatDate(photo.takenDate)} readOnly className="mt-1" />
                </div>

                {photo.width && photo.height && (
                    <div className="grid grid-cols-2 gap-2">
                      <div>
                        <Label className="text-slate-600">Width</Label>
                        <Input value={`${photo.width.toString()}px`} readOnly className="mt-1" />
                      </div>
                      <div>
                        <Label className="text-slate-600">Height</Label>
                        <Input value={`${photo.height.toString()}px`} readOnly className="mt-1" />
                      </div>
                    </div>
                )}

                {photo.scale && (
                    <div>
                      <Label className="text-slate-600">Scale</Label>
                      <Input value={photo.scale.toString()} readOnly className="mt-1" />
                    </div>
                )}

                {photo.orientation && (
                    <div>
                      <Label className="text-slate-600">Orientation</Label>
                      <Input value={photo.orientation.toString()} readOnly className="mt-1" />
                    </div>
                )}
              </CardContent>
            </Card>

            {/* Tags Section */}
            {photo.tags && photo.tags.length > 0 && (
                <Card className="shadow-lg">
                  <CardHeader>
                    <CardTitle className="text-lg text-slate-800">Tags</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="flex flex-wrap gap-2">
                      {photo.tags.map((tag, index) => (
                          <Badge key={index} variant="secondary" className="bg-blue-100 text-blue-800">
                            {tag}
                          </Badge>
                      ))}
                    </div>
                  </CardContent>
                </Card>
            )}

            {/* Captions Section */}
            {photo.captions && photo.captions.length > 0 && (
                <Card className="shadow-lg">
                  <CardHeader>
                    <CardTitle className="text-lg text-slate-800">Captions</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2">
                      {photo.captions.map((caption, index) => (
                          <Textarea
                              key={index}
                              value={caption}
                              readOnly
                              className="min-h-[60px] resize-none"
                          />
                      ))}
                    </div>
                  </CardContent>
                </Card>
            )}

            {/* Content Analysis Scores */}
            {(photo.adultScore !== undefined || photo.racyScore !== undefined) && (
                <Card className="shadow-lg">
                  <CardHeader>
                    <CardTitle className="text-lg text-slate-800">Content Analysis</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3">
                    {photo.adultScore !== undefined && (
                        <div>
                          <Label className="text-slate-600">Adult Score</Label>
                          <div className="flex items-center gap-2 mt-1">
                            <div className="flex-1 bg-slate-200 rounded-full h-2">
                              <div
                                  className="bg-orange-500 h-2 rounded-full transition-all duration-500"
                                  style={{ width: `${(photo.adultScore * 100).toString()}%` }}
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
                          <Label className="text-slate-600">Racy Score</Label>
                          <div className="flex items-center gap-2 mt-1">
                            <div className="flex-1 bg-slate-200 rounded-full h-2">
                              <div
                                  className="bg-red-500 h-2 rounded-full transition-all duration-500"
                                  style={{ width: `${(photo.racyScore * 100).toString()}%` }}
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
                <Card className="shadow-lg">
                  <CardHeader>
                    <CardTitle className="text-lg text-slate-800">
                      Detected Faces ({photo.faces.length})
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-sm text-slate-600 mb-3">
                      Hover over the blue boxes on the image to see face details.
                    </p>
                    <div className="space-y-2">
                      {photo.faces.map((face, index) => (
                          <div key={face.id || index} className="p-2 bg-slate-50 rounded text-sm">
                            <span className="font-medium">Face {index + 1}:</span>
                            <span className="ml-2 text-slate-600">
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
        </div>
      </div>
  );
};

export default PhotoDetailsPage;
