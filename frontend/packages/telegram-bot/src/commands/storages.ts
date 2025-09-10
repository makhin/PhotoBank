import type { MyContext } from '../i18n.js';
import { getAllStoragesWithPaths } from '../dictionaries.js';
import { parsePrefix, sendNamedItemsPage } from './helpers.js';

const MAX_PATHS_PER_STORAGE = 20;

export async function sendStoragesPage(
  ctx: MyContext,
  prefix: string,
  page: number,
  edit = false
) {
  await sendNamedItemsPage({
    ctx,
    command: 'storages',
    fetchAll: () =>
      Promise.resolve(
        getAllStoragesWithPaths().map((s) => {
          const paths = s.paths.slice(0, MAX_PATHS_PER_STORAGE);
          const rest = s.paths.length > MAX_PATHS_PER_STORAGE ? ['  ...'] : [];
          return {
            name: `${s.name}\n${paths.map((p) => `  ${p}`).concat(rest).join('\n')}`,
          };
        })
      ),
    prefix,
    page,
    edit,
    errorMsg: ctx.t('storages-error'),
  });
}

export async function storagesCommand(ctx: MyContext) {
  const prefix = parsePrefix(ctx.message?.text);
  await sendStoragesPage(ctx, prefix, 1);
}