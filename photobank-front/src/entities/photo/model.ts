export interface FaceBoxDto {
  top: string;
  left: string;
  width: string;
  height: string;
}

export interface FaceDto {
  id?: number;
  personId?: number;
  age?: number;
  gender?: boolean;
  faceAttributes?: string;
  faceBox: FaceBoxDto;
  friendlyFaceAttributes: string;
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

export interface PersonItemDto {
  personId?: number;
}

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

export interface PhotoItemDto {
  id?: number;
  thumbnail: string;
  name: string;
  takenDate?: string;
  isBW?: boolean;
  isAdultContent?: boolean;
  adultScore?: number;
  isRacyContent?: boolean;
  racyScore?: number;
  storageName: string;
  relativePath: string;
  tags?: TagItemDto[];
  persons?: PersonItemDto[];
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
}

export interface QueryResult {
  count?: number;
  photos?: PhotoItemDto[];
}

export interface TagItemDto {
  tagId?: number;
}
