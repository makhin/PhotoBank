import { fromUnixTime, isValid, parse, parseISO } from 'date-fns';

export type FlexibleDateInput = string | number | Date | null | undefined;

const UNIX_SECONDS_THRESHOLD = 1_000_000_000_000; // ~ Sat Nov 20 33658

const STRING_PARSE_PATTERNS = [
  'dd.MM.yyyy HH:mm:ss',
  'dd.MM.yyyy HH:mm',
  'dd.MM.yyyy',
];

export const DEFAULT_DATE_FORMAT = 'dd.MM.yyyy';
export const DEFAULT_DATE_TIME_FORMAT = 'dd.MM.yyyy HH:mm';

export function toDate(value: FlexibleDateInput): Date | null {
  if (value instanceof Date) {
    return isValid(value) ? value : null;
  }

  if (typeof value === 'number') {
    if (!Number.isFinite(value)) {
      return null;
    }

    if (Math.abs(value) < UNIX_SECONDS_THRESHOLD) {
      const unixDate = fromUnixTime(value);
      if (isValid(unixDate)) {
        return unixDate;
      }
    }

    const millisecondDate = new Date(value);
    return isValid(millisecondDate) ? millisecondDate : null;
  }

  if (typeof value === 'string') {
    const trimmed = value.trim();
    if (!trimmed) {
      return null;
    }

    const isoParsed = parseISO(trimmed);
    if (isValid(isoParsed)) {
      return isoParsed;
    }

    for (const pattern of STRING_PARSE_PATTERNS) {
      const parsed = parse(trimmed, pattern, new Date());
      if (isValid(parsed)) {
        return parsed;
      }
    }

    return null;
  }

  return null;
}
