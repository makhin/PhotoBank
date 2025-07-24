/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { PersonItemDto } from './PersonItemDto';
import type { TagItemDto } from './TagItemDto';
export type PhotoItemDto = {
    id: number;
    thumbnail: string;
    name: string;
    takenDate?: string | null;
    isBW?: boolean;
    isAdultContent?: boolean;
    adultScore?: number;
    isRacyContent?: boolean;
    racyScore?: number;
    storageName: string;
    relativePath: string;
    tags?: Array<TagItemDto> | null;
    persons?: Array<PersonItemDto> | null;
    captions?: Array<string> | null;
};

