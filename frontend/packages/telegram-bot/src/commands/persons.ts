import { Context } from "grammy";

import { getAllPersons } from '../dictionaries';
import { parsePrefix, sendNamedItemsPage } from "./helpers";

export async function sendPersonsPage(
  ctx: Context,
  prefix: string,
  page: number,
  edit = false,
) {
  await sendNamedItemsPage({
    ctx,
    command: "persons",
    fetchAll: () => getAllPersons(),
    prefix,
    page,
    edit,
    errorMsg: "🚫 Не удалось получить список персон.",
    filter: (p: { id: number }) => p.id >= 1,
  });
}

export async function personsCommand(ctx: Context) {
  const prefix = parsePrefix(ctx.message?.text);
  await sendPersonsPage(ctx, prefix, 1);
}

