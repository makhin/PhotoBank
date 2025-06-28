import { FaceDto } from './FaceDto';

export interface PhotoDto {
  id?: number;
  name: string;
  scale?: number;
  takenDate?: string;
  previewImage: string;
  orientation?: number;
  faces?: FaceDto[];
  captions?: string[];
  tags?: string[];
  adultScore?: number;
  racyScore?: number;
  height?: number;
  width?: number;
}