import { Context } from "grammy";
import { PersonsService } from "@photobank/shared/generated";
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
    fetchAll: PersonsService.getApiPersons,
    prefix,
    page,
    edit,
    errorMsg: "🚫 Не удалось получить список персон.",
    filter: (p) => (p as any).id >= 1,
  });
}

export async function personsCommand(ctx: Context) {
  const prefix = parsePrefix(ctx.message?.text);
  await sendPersonsPage(ctx, prefix, 1);
}

