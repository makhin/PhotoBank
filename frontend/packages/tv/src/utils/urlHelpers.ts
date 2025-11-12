// src/utils/urlHelpers.ts

/**
 * Базовый URL сервера (без /api)
 */
const BASE_SERVER_URL = 'https://makhin.ddns.net';

/**
 * Преобразует относительный URL в полный URL
 * Backend возвращает относительные пути типа /minio/bucket/key,
 * которые нужно преобразовать в полные URL для React Native
 *
 * @param url - URL который может быть относительным или полным
 * @returns Полный URL или null если url пустой
 *
 * @example
 * resolveMediaUrl('/minio/bucket/image.jpg')
 * // => 'https://makhin.ddns.net/minio/bucket/image.jpg'
 *
 * resolveMediaUrl('https://example.com/image.jpg')
 * // => 'https://example.com/image.jpg'
 *
 * resolveMediaUrl(null)
 * // => null
 */
export const resolveMediaUrl = (url?: string | null): string | undefined => {
  if (!url) {
    return undefined;
  }

  // Если URL уже полный (начинается с http:// или https://), возвращаем как есть
  if (url.startsWith('http://') || url.startsWith('https://')) {
    return url;
  }

  // Если URL относительный (начинается с /), добавляем базовый URL
  if (url.startsWith('/')) {
    return `${BASE_SERVER_URL}${url}`;
  }

  // В остальных случаях возвращаем как есть
  return url;
};
