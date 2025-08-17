import type { MyContext } from '../i18n';
import { parseQueryWithOpenAI } from '@photobank/shared/ai/openai';
import type { FilterDto } from '@photobank/shared/api/photobank';
import { getFilterHash } from '@photobank/shared/index';

import {
  findBestPersonId,
  findBestTagId,
} from '../dictionaries';
import { sendPhotosPage } from './photosPage';
import { logger } from '../logger';

export const aiFilters = new Map<string, FilterDto>();

export function parseAiPrompt(text?: string): string | null {
  if (!text) return null;
  const match = text.match(/^\/ai\s+([\s\S]+)/); // capture anything after /ai
  if (!match) return null;
  return match[1].trim();
}

export async function sendAiPage(
  ctx: MyContext,
  hash: string,
  page: number,
  edit = false
) {
  const filter = aiFilters.get(hash);
  if (!filter) {
    await ctx.reply(ctx.t('sorry-try-later'));
    return;
  }
  await sendPhotosPage({
    ctx,
    filter,
    page,
    edit,
    fallbackMessage: ctx.t('search-photos-empty'),
    buildCallbackData: (p) => `ai:${p}:${hash}`,
  });
}

export async function aiCommand(ctx: MyContext, promptOverride?: string) {
  const prompt = promptOverride ?? parseAiPrompt(ctx.message?.text);
  if (!prompt) {
    await ctx.reply(ctx.t('ai-usage'));
    return;
  }
  try {
    const filter = await parseQueryWithOpenAI(prompt);
    const dto: FilterDto = {} as FilterDto;
    const personIds = filter.persons
      .map((name) => findBestPersonId(name))
      .filter((id): id is number => typeof id === 'number');
    if (personIds.length) dto.persons = personIds;

    const tagIds = filter.tags
      .map((name) => findBestTagId(name))
      .filter((id): id is number => typeof id === 'number');
    if (tagIds.length) dto.tags = tagIds;

    if (filter.dateFrom) dto.takenDateFrom = filter.dateFrom.toISOString();
    if (filter.dateTo) dto.takenDateTo = filter.dateTo.toISOString();

    if (Object.keys(dto).length === 0) {
      await ctx.reply(ctx.t('ai-filter-empty'));
      return;
    }

    const hash = getFilterHash(dto);
    aiFilters.set(hash, dto);

    await sendAiPage(ctx, hash, 1);
  } catch (err) {
    logger.error(err);
    await ctx.reply(ctx.t('sorry-try-later'));
  }
}
