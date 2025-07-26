import { Context, InlineKeyboard } from "grammy";
import { prevPageText, nextPageText } from "@photobank/shared/constants";

export const PAGE_SIZE = 10;

export function parsePrefix(text?: string): string {
  if (!text) return "";
  const parts = text.split(" ");
  return parts[1]?.trim() ?? "";
}

export interface NamedItem { name: string }

export interface SendPageOptions<T extends NamedItem> {
  ctx: Context;
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
    console.error(err);
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
  lines.push("", `📄 Страница ${pageIndex} из ${totalPages}`);

  const keyboard = new InlineKeyboard();
  if (pageIndex > 1)
    keyboard.text(
      prevPageText,
      `${command}:${pageIndex - 1}:${encodeURIComponent(prefix)}`,
    );
  if (pageIndex < totalPages)
    keyboard.text(
      nextPageText,
      `${command}:${pageIndex + 1}:${encodeURIComponent(prefix)}`,
    );

  const text = lines.join("\n");

  if (edit) {
    await ctx
      .editMessageText(text, { reply_markup: keyboard })
      .catch(() => ctx.reply(text, { reply_markup: keyboard }));
  } else {
    await ctx.reply(text, { reply_markup: keyboard });
  }
}
