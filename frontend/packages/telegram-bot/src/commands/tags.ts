import { Context } from "grammy";
import { TagsService } from "@photobank/shared/generated";
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
    fetchAll: TagsService.getApiTags,
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

