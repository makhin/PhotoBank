import { Context } from "grammy";
import {
  searchPhotosEmptyMsg,
  searchCommandUsageMsg,
} from "@photobank/shared/constants";

import { sendPhotosPage } from "./photosPage";

function parseCaption(text?: string): string {
  if (!text) return "";
  const match = text.match(/^\/search\s+(.+)/);
  let caption = match ? match[1].trim() : "";
  if (
    (caption.startsWith('"') && caption.endsWith('"')) ||
    (caption.startsWith("'") && caption.endsWith("'"))
  ) {
    caption = caption.slice(1, -1);
  }
  return caption;
}

export async function handleSearch(ctx: Context) {
  const caption = parseCaption(ctx.message?.text);
  if (!caption) {
    await ctx.reply(searchCommandUsageMsg);
    return;
  }
  await sendSearchPage(ctx, caption, 1);
}

export const searchCommand = handleSearch;

export async function sendSearchPage(
  ctx: Context,
  caption: string,
  page: number,
  edit = false,
) {
  await sendPhotosPage({
    ctx,
    filter: { caption },
    page,
    edit,
    fallbackMessage: searchPhotosEmptyMsg,
    buildCallbackData: (p) => `search:${p}:${encodeURIComponent(caption)}`,
  });
}
