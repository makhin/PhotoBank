import type { MyContext } from "../i18n.js";
import { getAllPersons } from '../dictionaries.js';
import { parsePrefix, sendNamedItemsPage } from "./helpers.js";

export async function sendPersonsPage(
  ctx: MyContext,
  prefix: string,
  page: number,
  edit = false,
) {
  await sendNamedItemsPage({
    ctx,
    command: "persons",
    fetchAll: () => Promise.resolve(getAllPersons()),
    prefix,
    page,
    edit,
    errorMsg: ctx.t('persons-error'),
    filter: (p) => p.id >= 1,
  });
}

export async function personsCommand(ctx: MyContext) {
  const prefix = parsePrefix(ctx.message?.text);
  await sendPersonsPage(ctx, prefix, 1);
}

