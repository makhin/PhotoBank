import type { Context } from "grammy";
import type { FilterDto } from "@photobank/shared/api/photobank";

// ⚠️ проверь путь: файл, который ты прислал, называется dictionary.ts и лежит рядом с i18n.
// Если другая структура — поправь импорт.
import {
  setDictionariesUser,
  loadDictionaries,
  findBestPersonId,
  findBestTagId,
} from "../dictionary";

/** Черновик: как в прошлой версии — добавляем имена до резолва */
export type FilterDraft = FilterDto & {
  tagNames?: string[];
  personNames?: string[];
};

/** Утилиты */
const uniq = <T,>(xs: (T | null | undefined)[]) =>
  Array.from(new Set(xs.filter((x): x is T => x !== null && x !== undefined)));

function asIdsOrUndefined(xs: number[] | undefined): number[] | undefined {
  return xs && xs.length ? Array.from(new Set(xs)) : undefined;
}

/**
 * Резолвит имена (@alice, #family) → ID, используя локальные словари.
 * - Учитывает пользователя и локаль (через setDictionariesUser)
 * - Грузит словари при первом обращении (loadDictionaries)
 * - Использует fuzzy-поиск findBest* (порог в dictionary.ts = 0.5)
 * - Не найденные имена тихо игнорирует (можно расширить логированием)
 */
export async function resolveHumanNamesToIds(
  ctx: Context,
  draft: FilterDraft,
): Promise<FilterDto> {
  // Привязываем словари к текущему пользователю/локали
  const userId = ctx.from?.id ?? "";
  const locale = ctx.from?.language_code; // например: "en", "ru", "uk"
  setDictionariesUser(userId, locale);

  // Подтянуть словари (кэшируется per-user внутри dictionary.ts)
  await loadDictionaries(ctx as any);

  const curTagIds = uniq(draft.tags ?? []);
  const curPersonIds = uniq(draft.persons ?? []);

  const tagIdsFromNames = uniq(
    (draft.tagNames ?? [])
      .map((n) => (n ?? "").trim())
      .filter(Boolean)
      .map((n) => findBestTagId(n))
  ).filter((x): x is number => typeof x === "number");

  const personIdsFromNames = uniq(
    (draft.personNames ?? [])
      .map((n) => (n ?? "").trim())
      .filter(Boolean)
      .map((n) => findBestPersonId(n))
  ).filter((x): x is number => typeof x === "number");

  const {
    tagNames: _dropTagNames,
    personNames: _dropPersonNames,
    ...rest
  } = draft;

  return {
    ...rest,
    tags: asIdsOrUndefined([...curTagIds, ...tagIdsFromNames]),
    persons: asIdsOrUndefined([...curPersonIds, ...personIdsFromNames]),
  };
}