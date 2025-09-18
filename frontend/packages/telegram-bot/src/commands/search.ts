import {
  endOfDay,
  endOfMonth,
  endOfYear,
  formatISO,
  isValid,
  parse as parseDfns,
  parseISO,
  startOfDay,
  startOfMonth,
  startOfYear,
} from 'date-fns';
import type { FilterDto } from '@photobank/shared/api/photobank';

import type { MyContext } from '@/i18n';

import { sendPhotosPage } from './photosPage';

/* ===================== токенизация ===================== */

function tokenize(input: string): string[] {
  const tokens: string[] = [];
  let cur = "";
  let q: "'" | '"' | null = null;

  for (let i = 0; i < input.length; i++) {
    const ch = input.charAt(i);
    if (q) {
      if (ch === "\\" && i + 1 < input.length) cur += input.charAt(++i);
      else if (ch === q) q = null;
      else cur += ch;
    } else {
      if (ch === "'" || ch === '"') {
        q = ch;
        if (cur) {
          tokens.push(cur);
          cur = "";
        }
      } else if (/\s/.test(ch)) {
        if (cur) {
          tokens.push(cur);
          cur = "";
        }
      } else {
        cur += ch;
      }
    }
  }
  if (cur) tokens.push(cur);
  return tokens;
}

/* ===================== даты через date-fns ===================== */
/**
 * Поддерживаемые формы:
 *  - "2020"       → [2020-01-01 .. 2020-12-31]
 *  - "2020-07"    → [2020-07-01 .. 2020-07-31]
 *  - "2020-07-15" → [2020-07-15 .. 2020-07-15]
 */
function parseSingleLoose(str: string): { from: Date; to: Date } | null {
  // yyyy
  if (/^\d{4}$/.test(str)) {
    const d = parseDfns(str, "yyyy", new Date());
    if (!isValid(d)) return null;
    return { from: startOfYear(d), to: endOfYear(d) };
  }
  // yyyy-MM
  if (/^\d{4}-(0[1-9]|1[0-2])$/.test(str)) {
    const d = parseDfns(str, "yyyy-MM", new Date());
    if (!isValid(d)) return null;
    return { from: startOfMonth(d), to: endOfMonth(d) };
  }
  // yyyy-MM-dd
  if (/^\d{4}-(0[1-9]|1[0-2])-(0[1-9]|[12]\d|3[01])$/.test(str)) {
    const d = parseDfns(str, "yyyy-MM-dd", new Date());
    if (!isValid(d)) return null;
    return { from: d, to: d };
  }
  return null;
}

/** Диапазон A..B или одиночная дата */
function parseDateExpr(expr: string): { from?: Date; to?: Date } {
  if (!expr) return {};
  if (expr.includes("..")) {
    const [a, b] = expr.split("..").map((s) => s.trim());
    const left = a ? parseSingleLoose(a) : null;
    const right = b ? parseSingleLoose(b) : null;
    const res: { from?: Date; to?: Date } = {};
    if (left) res.from = startOfDay(left.from);
    if (right) res.to = endOfDay(right.to);
    return res;
  }
  const one = parseSingleLoose(expr);
  if (!one) return {};
  return {
    from: startOfDay(one.from),
    to: endOfDay(one.to),
  };
}

/** before:VAL → только верхняя граница */
function parseBefore(val?: string): { to?: Date } {
  if (!val) return {};
  const p = parseSingleLoose(val);
  return p ? { to: endOfDay(p.to) } : {};
}

/** after:VAL → только нижняя граница */
function parseAfter(val?: string): { from?: Date } {
  if (!val) return {};
  const p = parseSingleLoose(val);
  return p ? { from: startOfDay(p.from) } : {};
}

/* ===================== парсер → FilterDto ===================== */
/**
 * Поддержка:
 *  - caption: свободный текст
 *  - теги: #family, tags:family,kids
 *  - люди: @alice, people:alice,bob
 *  - даты: date:A | date:A..B | одиночная/диапазон без ключа
 *  - before:/after:
 *  - sort:(relevance|date_asc|date_desc) → orderBy
 *
 * ⚠️ В фильтре используем только имена.
 */
function parseArgsToFilter(raw: string): FilterDto {
  const tokens = tokenize(raw);

  const tagNames: string[] = [];
  const personNames: string[] = [];

  const rest: string[] = [];
  let takenDateFrom: Date | undefined;
  let takenDateTo: Date | undefined;

  for (const t of tokens) {
    const hasColon = t.includes(':');

    let key = '';
    let val: string | undefined;
    if (hasColon) {
      const [kRaw, ...vParts] = t.split(':');
      key = (kRaw ?? '').toLowerCase();
      val = vParts.join(':');
    }

    // #... / @... (имена)
    if (!hasColon && t.startsWith('#')) {
      const body = t.slice(1).trim();
      if (body && !tagNames.includes(body)) tagNames.push(body);
      continue;
    }
    if (!hasColon && t.startsWith('@')) {
      const body = t.slice(1).trim();
      if (body && !personNames.includes(body)) personNames.push(body);
      continue;
    }

    if (hasColon) {
      switch (key) {
        case 'tag':
        case 'tags': {
          (val ?? '')
            .split(',')
            .map((x) => x.trim().replace(/^#/, ''))
            .forEach((x) => {
              if (x && !tagNames.includes(x)) tagNames.push(x);
            });
          break;
        }
        case 'person':
        case 'people': {
          (val ?? '')
            .split(',')
            .map((x) => x.trim().replace(/^@/, ''))
            .forEach((x) => {
              if (x && !personNames.includes(x)) personNames.push(x);
            });
          break;
        }
        case 'date': {
          const { from, to } = parseDateExpr(val ?? '');
          if (from) takenDateFrom = from;
          if (to) takenDateTo = to;
          break;
        }
        case 'before': {
          const { to } = parseBefore(val);
          if (to) takenDateTo = to;
          break;
        }
        case 'after': {
          const { from } = parseAfter(val);
          if (from) takenDateFrom = from;
          break;
        }
        default:
          rest.push(t);
      }
    } else {
      // даты без ключа — одиночная/диапазон
      if (/^\d{4}(-\d{2}(-\d{2})?)?$/.test(t) || t.includes('..')) {
        const { from, to } = parseDateExpr(t);
        if (from) takenDateFrom = from;
        if (to) takenDateTo = to;
      } else {
        rest.push(t);
      }
    }
  }

  const caption = rest.join(" ").trim() || undefined;

  const filter: FilterDto = {};

  if (caption) filter.caption = caption;
  if (tagNames.length) filter.tagNames = Array.from(new Set(tagNames));
  if (personNames.length) filter.personNames = Array.from(new Set(personNames));
  if (takenDateFrom) filter.takenDateFrom = takenDateFrom;
  if (takenDateTo) filter.takenDateTo = takenDateTo;

  return filter;
}

/* ===================== callback_data кодек ===================== */

type SerializableFilter = Omit<FilterDto, "takenDateFrom" | "takenDateTo"> & {
  takenDateFrom?: string | null;
  takenDateTo?: string | null;
};

function encodeFilter(filter: FilterDto): string {
  const payload: SerializableFilter = {
    ...filter,
    takenDateFrom:
      filter.takenDateFrom instanceof Date
        ? formatISO(filter.takenDateFrom)
        : filter.takenDateFrom ?? undefined,
    takenDateTo:
      filter.takenDateTo instanceof Date
        ? formatISO(filter.takenDateTo)
        : filter.takenDateTo ?? undefined,
  };
  return Buffer.from(JSON.stringify(payload), "utf8").toString("base64url");
}
function decodeFilter(b64: string): FilterDto | null {
  try {
    const payload = JSON.parse(
      Buffer.from(b64, "base64url").toString("utf8"),
    ) as SerializableFilter;

    const filter: FilterDto = { ...payload };

    if (typeof payload.takenDateFrom === "string") {
      const parsed = parseISO(payload.takenDateFrom);
      if (isValid(parsed)) filter.takenDateFrom = parsed;
      else delete filter.takenDateFrom;
    } else if (payload.takenDateFrom === null) {
      filter.takenDateFrom = null;
    }

    if (typeof payload.takenDateTo === "string") {
      const parsed = parseISO(payload.takenDateTo);
      if (isValid(parsed)) filter.takenDateTo = parsed;
      else delete filter.takenDateTo;
    } else if (payload.takenDateTo === null) {
      filter.takenDateTo = null;
    }

    return filter;
  } catch {
    return null;
  }
}

/** Извлекаем сырые аргументы: ctx.match (grammY) или текст /search ... */
function extractRawArgs(ctx: MyContext): string {
  const anyCtx = ctx as unknown as { match?: string };
  if (typeof anyCtx.match === "string" && anyCtx.match.trim()) return anyCtx.match.trim();

  const text = ctx.message?.text ?? "";
  const m = text.match(/^\/search(@\w+)?\s+([\s\S]+)/i);
  return m?.[2]?.trim() ?? "";
}

/* ===================== публичные обработчики ===================== */

export async function handleSearch(ctx: MyContext) {
  const raw = extractRawArgs(ctx);
  if (!raw) {
    await ctx.reply(ctx.t("search-usage"));
    return;
  }

  const filter = parseArgsToFilter(raw);

  const nothingSet =
    !filter.caption &&
    (!filter.tagNames || filter.tagNames.length === 0) &&
    (!filter.personNames || filter.personNames.length === 0) &&
    !filter.takenDateFrom &&
    !filter.takenDateTo;

  if (nothingSet) {
    await ctx.reply(ctx.t("search-usage"));
    return;
  }

  await sendSearchPage(ctx, filter, 1);
}

export const searchCommand = handleSearch;

/** callback_data: "search:<page>:<base64url(JSON(FilterDto))>" */
export async function sendSearchPage(
  ctx: MyContext,
  filter: FilterDto,
  page: number,
  edit = false,
) {
  const payload = encodeFilter(filter);
  await sendPhotosPage({
    ctx,
    filter,
    page,
    edit,
    fallbackMessage: ctx.t("search-photos-empty"),
    buildCallbackData: (p) => `search:${p}:${payload}`,
  });
}

export function decodeSearchCallback(data: string): { page: number; filter: FilterDto } | null {
  if (!data.startsWith("search:")) return null;
  const parts = data.split(":");
  if (parts.length !== 3) return null;

  const page = Number(parts[1]);
  if (!Number.isInteger(page) || page < 1) return null;

  const filter = parts[2] ? decodeFilter(parts[2]) : null;
  if (!filter) return null;

  return { page, filter };
}