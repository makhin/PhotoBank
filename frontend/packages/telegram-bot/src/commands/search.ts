import {
  endOfDay,
  endOfMonth,
  endOfYear,
  isValid,
  parse as parseDfns,
  startOfDay,
  startOfMonth,
  startOfYear,
  subMilliseconds,
} from 'date-fns';
import type { FilterDto } from '@photobank/shared/api/photobank';

import type { MyContext } from '@/i18n';
import { sendFilterPage, decodeFilterCallback } from './filterPage';

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
  if (!p) return {};
  const start = startOfDay(p.from);
  return { to: subMilliseconds(start, 1) };
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
export function parseArgsToFilter(raw: string): FilterDto {
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

export async function sendSearchPage(
  ctx: MyContext,
  filter: FilterDto,
  page: number,
  edit = false,
) {
  await sendFilterPage({
    ctx,
    filter,
    page,
    edit,
    fallbackMessage: ctx.t("search-photos-empty"),
    callbackPrefix: "search",
  });
}

export function decodeSearchCallback(data: string): { page: number; filter: FilterDto } | null {
  return decodeFilterCallback("search", data);
}