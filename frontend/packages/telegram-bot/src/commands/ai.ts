import { parseQueryWithOpenAI } from '@photobank/shared/ai/openai';
import type { FilterDto } from '@photobank/shared/api/photobank';
import type { MyContext } from '../i18n';
import { logger } from '../logger';
import { decodeFilterCallback, sendFilterPage } from './filterPage';

export function parseAiPrompt(text?: string): string | null {
  if (!text) return null;
  const match = text.match(/^\/ai\s+([\s\S]+)/); // capture anything after /ai
  return match?.[1]?.trim() ?? null;
}

export async function sendAiPage(
  ctx: MyContext,
  filter: FilterDto,
  page: number,
  edit = false,
) {
  await sendFilterPage({
    ctx,
    filter,
    page,
    edit,
    fallbackMessage: ctx.t('search-photos-empty'),
    callbackPrefix: 'ai',
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
    const dto: FilterDto = {};

    if (filter.personNames.length) dto.personNames = filter.personNames;
    if (filter.tagNames.length) dto.tagNames = filter.tagNames;
    if (filter.dateFrom) dto.takenDateFrom = filter.dateFrom;
    if (filter.dateTo) dto.takenDateTo = filter.dateTo;

    if (
      !dto.personNames &&
      !dto.tagNames &&
      !dto.takenDateFrom &&
      !dto.takenDateTo
    ) {
      await ctx.reply(ctx.t('ai-filter-empty'));
      return;
    }

    await sendAiPage(ctx, dto, 1);
  } catch (err: unknown) {
    logger.error(err);
    await ctx.reply(ctx.t('sorry-try-later'));
  }
}

export function decodeAiCallback(data: string): { page: number; filter: FilterDto } | null {
  return decodeFilterCallback('ai', data);
}
