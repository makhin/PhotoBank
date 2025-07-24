/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { FaceDto } from './FaceDto';
import type { GeoPointDto } from './GeoPointDto';
export type PhotoDto = {
    id: number;
    name: string;
    scale?: number;
    takenDate?: string | null;
    previewImage: string;
    location?: GeoPointDto;
    orientation?: number | null;
    faces?: Array<FaceDto> | null;
    captions?: Array<string> | null;
    tags?: Array<string> | null;
    adultScore?: number;
    racyScore?: number;
    height?: number;
    width?: number;
};

