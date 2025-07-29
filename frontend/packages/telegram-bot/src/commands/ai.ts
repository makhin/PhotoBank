import { Context } from 'grammy';
import {
  aiCommandUsageMsg,
  sorryTryToRequestLaterMsg,
} from '@photobank/shared/constants';
import { parseQueryWithOpenAI } from '@photobank/shared/ai/openai';
import { findBestPersonId, findBestTagId } from '@photobank/shared/dictionaries';
import type { FilterDto } from '@photobank/shared/generated';

export function parseAiPrompt(text?: string): string | null {
    if (!text) return null;
    const match = text.match(/^\/ai\s+([\s\S]+)/); // capture anything after /ai
    if (!match) return null;
    return match[1].trim();
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

    await ctx.reply(JSON.stringify(dto, null, 2));
  } catch (err) {
    console.error(err);
    await ctx.reply(sorryTryToRequestLaterMsg);
  }
}
