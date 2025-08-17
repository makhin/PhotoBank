import type { MyContext } from "../i18n";
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

export async function handleSearch(ctx: MyContext) {
  const caption = parseCaption(ctx.message?.text);
  if (!caption) {
    await ctx.reply(ctx.t('search-usage'));
    return;
  }
  await sendSearchPage(ctx, caption, 1);
}

export const searchCommand = handleSearch;

export async function sendSearchPage(
  ctx: MyContext,
  caption: string,
  page: number,
  edit = false,
) {
  await sendPhotosPage({
    ctx,
    filter: { caption },
    page,
    edit,
    fallbackMessage: ctx.t('search-photos-empty'),
    buildCallbackData: (p) => `search:${p}:${encodeURIComponent(caption)}`,
  });
}
