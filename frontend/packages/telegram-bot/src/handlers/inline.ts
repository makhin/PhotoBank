import type { InlineQueryResult, InlineQueryResultPhoto } from 'grammy/types';
import { formatDate } from '@photobank/shared/format';
import { ProblemDetailsError } from '@photobank/shared/types/problem';

import { bot } from '../bot';
import { ensureUserAccessToken } from '../auth';
import { searchPhotos } from '../services/photo';
import { getTagName } from '../dictionaries';
import { logger } from '../logger';
import type { MyContext } from '../i18n';
import type { PhotoItemDto } from '../types';
import type { PhotoItemDtoPageResponse } from '../api/photobank/photoBankApi.schemas';

const PAGE_SIZE = 20;

bot.on('inline_query', async (ctx: MyContext) => {
  const q = (ctx.inlineQuery?.query ?? '').trim();
  const offset = Number(ctx.inlineQuery?.offset ?? '0') || 0;
  const page = Math.floor(offset / PAGE_SIZE) + 1;

  // Авторизация для inline: если нет — мягко предлагаем /start link
  try {
    await ensureUserAccessToken(ctx);
  } catch (e: unknown) {
    await ctx.answerInlineQuery(
      [],
      {
        is_personal: true,
        cache_time: 2,
        button: {
          text: ctx.t('deeplink-not-linked'),
          start_parameter: 'link',
        },
      } satisfies Parameters<typeof ctx.answerInlineQuery>[1],
    );
    return;
  }
  try {
    const resp = await searchPhotos(ctx, {
      caption: q,
      page,
      pageSize: PAGE_SIZE,
    });

    const pageItems = resp.items ?? undefined;
    const legacyItems = hasLegacyPhotos(resp) ? resp.photos ?? [] : undefined;
    const items: PhotoItemDto[] = pageItems ?? legacyItems ?? [];

    const results: InlineQueryResult[] = items.map(
      (p): InlineQueryResultPhoto => ({
        type: 'photo',
        id: String(p.id),
        photo_url: p.thumbnailUrl ?? '',
        thumbnail_url: p.thumbnailUrl ?? '',
        title: p.name ?? `#${p.id}`,
        description: [
          formatDate(p.takenDate),
          (p.tags ?? [])
            .slice(0, 3)
            .map((tagId) => getTagName(tagId))
            .filter(Boolean)
            .join(', '),
        ]
          .filter(Boolean)
          .join(' • '),
        caption: `${p.name ?? ''}\n${formatDate(p.takenDate) ?? ''}`.trim(),
      }),
    );

    const nextOffset = items.length === PAGE_SIZE ? String(offset + PAGE_SIZE) : '';

    await ctx.answerInlineQuery(
      results,
      {
        is_personal: true,
        cache_time: 5,
        next_offset: nextOffset,
      } satisfies Parameters<typeof ctx.answerInlineQuery>[1],
    );
  } catch (e: unknown) {
    let forbidden = false;
    if (e instanceof ProblemDetailsError) {
      forbidden = e.problem.status === 403;
    }
    if (forbidden) {
      await ctx.answerInlineQuery(
        [],
        {
          is_personal: true,
          cache_time: 0,
          button: {
            text: ctx.t('inline-link-account'),
            start_parameter: 'link',
          },
        } satisfies Parameters<typeof ctx.answerInlineQuery>[1],
      );
      return;
    }
    logger.warn('inline_query error', e);
    await ctx.answerInlineQuery(
      [],
      {
        is_personal: true,
        cache_time: 0,
        button: {
          text: ctx.t('inline-search-failed'),
          start_parameter: 'help',
        },
      } satisfies Parameters<typeof ctx.answerInlineQuery>[1],
    );
  }
});

function hasLegacyPhotos(
  response: PhotoItemDtoPageResponse,
): response is PhotoItemDtoPageResponse & { photos?: PhotoItemDto[] | null } {
  return 'photos' in response;
}
