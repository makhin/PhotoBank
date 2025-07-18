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

// Telegram bot messages
export const welcomeBotMsg = "Добро пожаловать. Запущен и работает!";
export const captionMissingMsg = "Без подписи.";
export const unknownMessageReplyMsg = "Получил другое сообщение!";
export const photoCommandUsageMsg = "❗ Используй: /photo <id>";
export const photoNotFoundMsg = "❌ Фото не найдено.";
export const todaysPhotosEmptyMsg = "📭 Сегодняшних фото пока нет.";
export const unknownYearLabel = "Неизвестный год";
export const prevPageText = "◀ Назад";
export const nextPageText = "Вперёд ▶";
export const rolesLabel = "Роли:";
export const rolesEmptyLabel = "Роли отсутствуют.";
export const claimsLabel = "Права пользователя:";
export const claimsEmptyLabel = "Права пользователя отсутствуют.";
export const botTokenNotDefinedError = "BOT_TOKEN is not defined";
export const apiCredentialsNotDefinedError = "API_EMAIL or API_PASSWORD is not defined";
