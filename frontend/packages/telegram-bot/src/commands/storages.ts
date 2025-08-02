import { Context } from "grammy";
import { getAllStoragesWithPaths } from '@photobank/shared/dictionaries';
import { parsePrefix, sendNamedItemsPage } from "./helpers";

export async function sendStoragesPage(
  ctx: Context,
  prefix: string,
  page: number,
  edit = false,
) {
  await sendNamedItemsPage({
    ctx,
    command: "storages",
    fetchAll: async () =>
      getAllStoragesWithPaths().map((s) => ({
        name: `${s.name}\n${s.paths.map((p) => `  ${p}`).join("\n")}`,
      })),
    prefix,
    page,
    edit,
    errorMsg: "🚫 Не удалось получить список хранилищ.",
  });
}

export async function storagesCommand(ctx: Context) {
  const prefix = parsePrefix(ctx.message?.text);
  await sendStoragesPage(ctx, prefix, 1);
}
