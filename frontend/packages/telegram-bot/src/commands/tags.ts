import { Context } from "grammy";
import { getAllTags } from "@photobank/shared/api";
import { parsePrefix, sendNamedItemsPage } from "./helpers";

export async function sendTagsPage(
  ctx: Context,
  prefix: string,
  page: number,
  edit = false,
) {
  await sendNamedItemsPage({
    ctx,
    command: "tags",
    fetchAll: getAllTags,
    prefix,
    page,
    edit,
    errorMsg: "🚫 Не удалось получить список тегов.",
  });
}

export async function tagsCommand(ctx: Context) {
  const prefix = parsePrefix(ctx.message?.text);
  await sendTagsPage(ctx, prefix, 1);
}

