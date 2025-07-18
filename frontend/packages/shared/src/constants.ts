import type {FilterDto} from "@photobank/shared/types";

export const DEFAULT_PHOTO_FILTER: FilterDto = {
    thisDay: true,
    skip: 0,
    top: 10,
} as const;

export const getPhotoErrorMsg = "🚫 Не удалось получить фото.";
export const getProfileErrorMsg = "🚫 Не удалось получить профиль пользователя.";
export const sorryTryToRequestLaterMsg = "🚫 Извините, попробуйте позже.";
export const apiErrorMsg = "API error:";