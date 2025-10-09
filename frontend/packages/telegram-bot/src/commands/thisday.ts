import { createThisDayFilter } from '@photobank/shared';

import type { MyContext } from '../i18n';
import { sendPhotosPage } from './photosPage';

function parsePage(text?: string): number {
  if (!text) return 1;
  const match = text.match(/\/thisday\s+(\d+)/);
  const g1 = match?.[1];
  return g1 ? parseInt(g1, 10) || 1 : 1;
}

export async function handleThisDay(ctx: MyContext) {
  const page = parsePage(ctx.message?.text);
  await sendThisDayPage(ctx, page);
}

export const thisDayCommand = handleThisDay;
export async function sendThisDayPage(ctx: MyContext, page: number, edit = false) {
  const now = new Date();
  await sendPhotosPage({
    ctx,
    filter: createThisDayFilter(now),
    page,
    edit,
    fallbackMessage: ctx.t('todays-photos-empty'),
    buildCallbackData: (p) => `thisday:${p}`,
  });
}
