import { Context } from "grammy";
import { getAllStoragesWithPaths } from '@photobank/shared/dictionaries';
import { parsePrefix, sendNamedItemsPage } from "./helpers";

const MAX_PATHS_PER_STORAGE = 20;

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
      getAllStoragesWithPaths().map((s) => {
        const paths = s.paths.slice(0, MAX_PATHS_PER_STORAGE);
        const rest = s.paths.length > MAX_PATHS_PER_STORAGE ? ["  ..."] : [];
        return {
          name: `${s.name}\n${paths.map((p) => `  ${p}`).concat(rest).join("\n")}`,
        };
      }),
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
