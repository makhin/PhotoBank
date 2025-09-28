import type { FilterDto } from '@photobank/shared/api/photobank';

import type { MyContext } from '@/i18n';
import {
  registerSearchFilterToken,
  resolveSearchFilterToken,
} from '@/cache/searchFilterCache';

import { sendPhotosPage } from './photosPage';

const TELEGRAM_CALLBACK_LIMIT = 64;

interface SendFilterPageOptions {
  ctx: MyContext;
  filter: FilterDto;
  page: number;
  edit?: boolean;
  fallbackMessage: string;
  callbackPrefix: string;
}

export async function sendFilterPage({
  ctx,
  filter,
  page,
  edit = false,
  fallbackMessage,
  callbackPrefix,
}: SendFilterPageOptions) {
  const token = registerSearchFilterToken(filter);

  await sendPhotosPage({
    ctx,
    filter,
    page,
    edit,
    fallbackMessage,
    buildCallbackData: (p) => {
      const callbackData = `${callbackPrefix}:${p}:${token}`;
      if (callbackData.length > TELEGRAM_CALLBACK_LIMIT) {
        throw new Error(
          `${callbackPrefix} callback_data exceeds Telegram limit`,
        );
      }
      return callbackData;
    },
    saveLastFilterSource: callbackPrefix === 'ai' ? 'ai' : 'search',
  });
}

export function decodeFilterCallback(
  callbackPrefix: string,
  data: string,
): { page: number; filter: FilterDto } | null {
  if (!data.startsWith(`${callbackPrefix}:`)) return null;

  const parts = data.split(':');
  if (parts.length !== 3) return null;

  const page = Number(parts[1]);
  if (!Number.isInteger(page) || page < 1) return null;

  const token = parts[2];
  if (!token) return null;

  const filter = resolveSearchFilterToken(token);
  if (!filter) return null;

  return { page, filter };
}
