import type { FaceDto } from './FaceDto';
import type { GeoPointDto } from './GeoPointDto';

export interface PhotoDto {
  id: number;
  name: string;
  scale: number;
  takenDate?: string;
  previewImage: string;
  location?: GeoPointDto;
  orientation?: number;
  faces?: FaceDto[];
  captions?: string[];
  tags?: string[];
  adultScore: number;
  racyScore: number;
  height: number;
  width: number;
}