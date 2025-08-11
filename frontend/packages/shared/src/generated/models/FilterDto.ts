/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { PageRequest } from './PageRequest';
export type FilterDto = (PageRequest & {
    storages?: Array<number> | null;
    isBW?: boolean | null;
    isAdultContent?: boolean | null;
    isRacyContent?: boolean | null;
    relativePath?: string | null;
    paths?: Array<number> | null;
    caption?: string | null;
    takenDateFrom?: string | null;
    takenDateTo?: string | null;
    thisDay?: boolean | null;
    persons?: Array<number> | null;
    tags?: Array<number> | null;
    orderBy?: string | null;
});

