import { InlineKeyboard } from "grammy";
import type { MyContext } from "../i18n";

import { logger } from "../logger";

export const PAGE_SIZE = 10;

export function parsePrefix(text?: string): string {
  if (!text) return "";
  const parts = text.split(" ");
  return parts[1]?.trim() ?? "";
}

export interface NamedItem { name: string }

export interface SendPageOptions<T extends NamedItem> {
  ctx: MyContext;
  command: string;
  fetchAll: () => Promise<T[]>;
  prefix: string;
  page: number;
  edit?: boolean;
  errorMsg: string;
  filter?: (item: T) => boolean;
}

export async function sendNamedItemsPage<T extends NamedItem>({
  ctx,
  command,
  fetchAll,
  prefix,
  page,
  edit = false,
  errorMsg,
  filter,
}: SendPageOptions<T>) {
  let items: T[];
  try {
    items = await fetchAll();
  } catch (err) {
    logger.error(err);
    await ctx.reply(errorMsg);
    return;
  }

  if (filter) items = items.filter(filter);

  const filtered = items
    .filter((i) => i.name.toLowerCase().startsWith(prefix.toLowerCase()))
    .sort((a, b) => a.name.localeCompare(b.name));

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const pageIndex = Math.min(Math.max(page, 1), totalPages);
  const slice = filtered.slice(
    (pageIndex - 1) * PAGE_SIZE,
    pageIndex * PAGE_SIZE,
  );

  const lines = slice.map((i) => `- ${i.name}`);
  lines.push("", ctx.t('page-info', { page: pageIndex, total: totalPages }));

  const keyboard = new InlineKeyboard();
  if (pageIndex > 1) {
    keyboard.text(
      ctx.t('first-page'),
      `${command}:1:${encodeURIComponent(prefix)}`,
    );
    keyboard.text(
      ctx.t('prev-page'),
      `${command}:${pageIndex - 1}:${encodeURIComponent(prefix)}`,
    );
  }
  if (pageIndex < totalPages) {
    keyboard.text(
      ctx.t('next-page'),
      `${command}:${pageIndex + 1}:${encodeURIComponent(prefix)}`,
    );
    keyboard.text(
      ctx.t('last-page'),
      `${command}:${totalPages}:${encodeURIComponent(prefix)}`,
    );
  }

  const text = lines.join("\n");

  if (edit) {
    await ctx
      .editMessageText(text, { reply_markup: keyboard })
      .catch(() => ctx.reply(text, { reply_markup: keyboard }));
  } else {
    await ctx.reply(text, { reply_markup: keyboard });
  }
}
