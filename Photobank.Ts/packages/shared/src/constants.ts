import type { FilterDto } from "@photobank/shared/types";

export const DEFAULT_PHOTO_FILTER: FilterDto = {
    thisDay: true,
    skip: 0,
    top: 10,
} as const;
