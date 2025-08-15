import type { InlineQueryResult, InlineQueryResultPhoto } from 'grammy/types';
import { formatDate } from '@photobank/shared/format';
import { ProblemDetailsError } from '@photobank/shared/types/problem';

import { bot } from '../bot';
import { ensureUserAccessToken } from '../auth';
import { searchPhotos } from '../services/photo';
import { getTagName } from '../dictionaries';
import { logger } from '../logger';

const PAGE_SIZE = 20;

bot.on('inline_query', async (ctx) => {
  const q = (ctx.inlineQuery.query || '').trim();
  const offset = Number(ctx.inlineQuery.offset || '0') || 0;

  try {
    await ensureUserAccessToken(ctx);

    const resp = await searchPhotos(ctx, {
      caption: q,
      skip: offset,
      top: PAGE_SIZE,
    });

    const items = resp.data.photos ?? resp.data.items ?? resp.data ?? [];
    const results: InlineQueryResult[] = items.map((p): InlineQueryResultPhoto => ({
      type: 'photo',
      id: String(p.id),
      photo_url: p.previewUrl ?? p.originalUrl ?? '',
      thumb_url: p.thumbnailUrl ?? p.previewUrl ?? p.originalUrl ?? '',
      title: p.name ?? `#${p.id}`,
      description: [
        formatDate(p.takenDate),
        (p.tags ?? []).slice(0, 3).map(t => getTagName(t.tagId)).join(', '),
      ]
        .filter(Boolean)
        .join(' • '),
      caption: `${p.name ?? ''}\n${formatDate(p.takenDate) ?? ''}`.trim(),
    }));

    const nextOffset = items.length === PAGE_SIZE ? String(offset + PAGE_SIZE) : '';

    await ctx.answerInlineQuery(results, {
      is_personal: true,
      cache_time: 5,
      next_offset: nextOffset,
      switch_pm_text: undefined,
      switch_pm_parameter: undefined,
    });
  } catch (e) {
    if (e instanceof ProblemDetailsError && e.status === 403) {
      await ctx.answerInlineQuery([], {
        is_personal: true,
        cache_time: 0,
        switch_pm_text: 'Привяжите аккаунт, чтобы искать фото',
        switch_pm_parameter: 'link',
      });
      return;
    }
    logger.warn('inline_query error', e);
    await ctx.answerInlineQuery([], {
      is_personal: true,
      cache_time: 0,
      switch_pm_text: 'Не удалось выполнить поиск (повторить?)',
      switch_pm_parameter: 'help',
    });
  }
});
