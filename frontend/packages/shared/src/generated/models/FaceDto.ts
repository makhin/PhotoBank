/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { FaceBoxDto } from './FaceBoxDto';
export type FaceDto = {
    id?: number;
    personId?: number | null;
    age?: number | null;
    gender?: boolean | null;
    faceAttributes?: string | null;
    faceBox: FaceBoxDto;
    friendlyFaceAttributes: string;
};

