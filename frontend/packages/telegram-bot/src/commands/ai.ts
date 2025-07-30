import { Context } from 'grammy';
import {
  aiCommandUsageMsg,
  sorryTryToRequestLaterMsg,
  searchPhotosEmptyMsg,
} from '@photobank/shared/constants';
import { parseQueryWithOpenAI } from '@photobank/shared/ai/openai';
import { findBestPersonId, findBestTagId } from '@photobank/shared/dictionaries';
import type { FilterDto } from '@photobank/shared/generated';
import { getFilterHash } from '@photobank/shared/index';
import { sendPhotosPage } from './photosPage';

export const aiFilters = new Map<string, FilterDto>();

export function parseAiPrompt(text?: string): string | null {
  if (!text) return null;
  const match = text.match(/^\/ai\s+([\s\S]+)/); // capture anything after /ai
  if (!match) return null;
  return match[1].trim();
}

export async function sendAiPage(
  ctx: Context,
  hash: string,
  page: number,
  edit = false,
) {
  const filter = aiFilters.get(hash);
  if (!filter) {
    await ctx.reply(sorryTryToRequestLaterMsg);
    return;
  }
  await sendPhotosPage({
    ctx,
    filter,
    page,
    edit,
    fallbackMessage: searchPhotosEmptyMsg,
    buildCallbackData: (p) => `ai:${p}:${hash}`,
  });
}

export async function aiCommand(ctx: Context) {
  const prompt = parseAiPrompt(ctx.message?.text);
  if (!prompt) {
    await ctx.reply(aiCommandUsageMsg);
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

    const hash = await getFilterHash(dto);
    aiFilters.set(hash, dto);

    await sendAiPage(ctx, hash, 1);
  } catch (err) {
    console.error(err);
    await ctx.reply(sorryTryToRequestLaterMsg);
  }
}
