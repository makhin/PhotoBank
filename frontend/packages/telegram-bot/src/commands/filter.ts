import type { FilterDto } from '@photobank/shared/api/photobank';
import type { MyContext } from '../i18n';
import { getLastFilter } from '../cache/lastFilterCache';

function serializeFilter(filter: FilterDto): string {
  return JSON.stringify(
    filter,
    (_key, value) => (value instanceof Date ? value.toISOString() : value),
    2,
  );
}

export async function filterCommand(ctx: MyContext) {
  const chatId = ctx.chat?.id;
  if (!chatId) {
    await ctx.reply(ctx.t('chat-undetermined'));
    return;
  }

  const entry = getLastFilter(chatId);
  if (!entry) {
    await ctx.reply(ctx.t('filter-empty'));
    return;
  }

  const sourceKey = entry.source === 'ai' ? 'filter-source-ai' : 'filter-source-search';
  const source = ctx.t(sourceKey);
  const filterJson = serializeFilter(entry.filter);

  await ctx.reply(
    ctx.t('filter-last', {
      source,
      filter: filterJson,
    }),
  );
}
