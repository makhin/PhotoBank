import type { MyContext } from "../i18n";

import { getAllTags } from '../dictionaries';
import { parsePrefix, sendNamedItemsPage } from "./helpers";

export async function sendTagsPage(
  ctx: MyContext,
  prefix: string,
  page: number,
  edit = false,
) {
  await sendNamedItemsPage({
    ctx,
    command: "tags",
    fetchAll: () => Promise.resolve(getAllTags()),
    prefix,
    page,
    edit,
    errorMsg: ctx.t('tags-error'),
  });
}

export async function tagsCommand(ctx: MyContext) {
  const prefix = parsePrefix(ctx.message?.text);
  await sendTagsPage(ctx, prefix, 1);
}

