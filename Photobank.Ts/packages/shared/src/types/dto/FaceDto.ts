import { FaceBoxDto } from './FaceBoxDto';

export interface FaceDto {
  id?: number;
  personId?: number;
  age?: number;
  gender?: boolean;
  faceAttributes?: string;
  faceBox: FaceBoxDto;
  friendlyFaceAttributes: string;
}