export interface TagDto {
  id: number;
  name?: string;
}

export interface PersonDto {
  id: number;
  name?: string;
}

export interface StorageDto {
  id: number;
  name?: string;
}

export interface PathDto {
  storageId: number;
  path?: string;
}

export interface FaceBoxDto {
  top?: string;
  left?: string;
  width?: string;
  height?: string;
}

export interface FaceDto {
  id: number;
  personId?: number;
  age?: number;
  gender?: boolean;
  faceAttributes?: string;
  friendlyFaceAttributes?: string;
  faceBox?: FaceBoxDto;
}

export interface PhotoItemDto {
  id: number;
  thumbnail?: string;
  name?: string;
  takenDate?: string;
  isBW: boolean;
  isAdultContent: boolean;
  adultScore: number;
  isRacyContent: boolean;
  racyScore: number;
  storageName?: string;
  relativePath?: string;
  tags?: { tagId: number }[];
  persons?: { personId: number }[];
}

export interface PhotoDto {
  id: number;
  name?: string;
  scale: number;
  takenDate?: string;
  previewImage?: string;
  orientation?: number;
  height: number;
  width: number;
  faces?: FaceDto[];
  captions?: string[];
  tags?: string[];
  adultScore: number;
  racyScore: number;
}

export interface FilterDto {
  storages?: number[];
  isBW?: boolean;
  isAdultContent?: boolean;
  isRacyContent?: boolean;
  relativePath?: string;
  paths?: number[];
  caption?: string;
  takenDateFrom?: string;
  takenDateTo?: string;
  thisDay?: boolean;
  persons?: number[];
  tags?: number[];
  orderBy?: string;
  skip?: number;
  top?: number;
}

export interface QueryResult {
  count: number;
  photos?: PhotoItemDto[];
}