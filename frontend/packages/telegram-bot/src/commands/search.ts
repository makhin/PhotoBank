import {
  endOfMonth,
  endOfYear,
  format as formatDfns,
  isValid,
  parse as parseDfns,
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
    const ch = input[i];
    if (q) {
      if (ch === "\\" && i + 1 < input.length) cur += input[++i];
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
function parseDateExpr(expr: string): { from?: string; to?: string } {
  if (!expr) return {};
  if (expr.includes("..")) {
    const [a, b] = expr.split("..").map((s) => s.trim());
    const left = a ? parseSingleLoose(a) : null;
    const right = b ? parseSingleLoose(b) : null;
    return {
      from: left ? formatDfns(left.from, "yyyy-MM-dd") : undefined,
      to: right ? formatDfns(right.to, "yyyy-MM-dd") : undefined,
    };
  }
  const one = parseSingleLoose(expr);
  if (!one) return {};
  return {
    from: formatDfns(one.from, "yyyy-MM-dd"),
    to: formatDfns(one.to, "yyyy-MM-dd"),
  };
}

/** before:VAL → только верхняя граница */
function parseBefore(val?: string): { to?: string } {
  if (!val) return {};
  const p = parseSingleLoose(val);
  return p ? { to: formatDfns(p.to, "yyyy-MM-dd") } : {};
}

/** after:VAL → только нижняя граница */
function parseAfter(val?: string): { from?: string } {
  if (!val) return {};
  const p = parseSingleLoose(val);
  return p ? { from: formatDfns(p.from, "yyyy-MM-dd") } : {};
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
  let takenDateFrom: string | undefined;
  let takenDateTo: string | undefined;

  for (const t of tokens) {
    const hasColon = t.includes(":");
    const [kRaw, ...vParts] = t.split(":");
    const key = kRaw.toLowerCase();
    const val = hasColon ? vParts.join(":") : undefined;

    // #... / @... (имена)
    if (!hasColon && t.startsWith("#")) {
      const body = t.slice(1).trim();
      if (body && !tagNames.includes(body)) tagNames.push(body);
      continue;
    }
    if (!hasColon && t.startsWith("@")) {
      const body = t.slice(1).trim();
      if (body && !personNames.includes(body)) personNames.push(body);
      continue;
    }

    if (hasColon) {
      switch (key) {
        case "tag":
        case "tags": {
          (val ?? "")
            .split(",")
            .map((x) => x.trim().replace(/^#/, ""))
            .forEach((x) => {
              if (x && !tagNames.includes(x)) tagNames.push(x);
            });
        }
        case "person":
        case "people": {
          (val ?? "")
            .split(",")
            .map((x) => x.trim().replace(/^@/, ""))
            .forEach((x) => {
              if (x && !personNames.includes(x)) personNames.push(x);
            });
        }
        case "date": {
          const { from, to } = parseDateExpr(val ?? "");
          if (from) takenDateFrom = from;
          if (to) takenDateTo = to;
        }
        case "before": {
          const { to } = parseBefore(val);
          if (to) takenDateTo = to;
        }
        case "after": {
          const { from } = parseAfter(val);
          if (from) takenDateFrom = from
        }
        default:
          rest.push(t);
      }
    } else {
      // даты без ключа — одиночная/диапазон
      if (/^\d{4}(-\d{2}(-\d{2})?)?$/.test(t) || t.includes("..")) {
        const { from, to } = parseDateExpr(t);
        if (from) takenDateFrom = from;
        if (to) takenDateTo = to;
      } else {
        rest.push(t);
      }
    }
  }

  const caption = rest.join(" ").trim() || undefined;




  return {
    caption,
    tagNames: tagNames.length ? Array.from(new Set(tagNames)) : undefined,
    personNames: personNames.length
      ? Array.from(new Set(personNames))
      : undefined,
    takenDateFrom,
    takenDateTo,
  };
}

/* ===================== callback_data кодек ===================== */

function encodeFilter(filter: FilterDto): string {
  return Buffer.from(JSON.stringify(filter), "utf8").toString("base64url");
}
function decodeFilter(b64: string): FilterDto | null {
  try {
    return JSON.parse(Buffer.from(b64, "base64url").toString("utf8")) as FilterDto;
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

  const filter = decodeFilter(parts[2]);
  if (!filter) return null;

  return { page, filter };
}