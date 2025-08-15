export type DateInput = string | number | Date | null | undefined;

const locale =
  typeof navigator !== 'undefined' && navigator.language
    ? navigator.language
    : 'en-US';

const dateFormatterCache = new Map<string, Intl.DateTimeFormat>();
const numberFormatterCache = new Map<string, Intl.NumberFormat>();

function getDateFormatter(opts?: Intl.DateTimeFormatOptions) {
  const key = JSON.stringify(opts ?? {});
  let fmt = dateFormatterCache.get(key);
  if (!fmt) {
    fmt = new Intl.DateTimeFormat(locale, opts);
    dateFormatterCache.set(key, fmt);
  }
  return fmt;
}

function getNumberFormatter(opts?: Intl.NumberFormatOptions) {
  const key = JSON.stringify(opts ?? {});
  let fmt = numberFormatterCache.get(key);
  if (!fmt) {
    fmt = new Intl.NumberFormat(locale, opts);
    numberFormatterCache.set(key, fmt);
  }
  return fmt;
}

export function formatDate(
  d: DateInput,
  opts?: Intl.DateTimeFormatOptions
): string {
  if (d === null || d === undefined) return '';
  const date = typeof d === 'string' || typeof d === 'number' ? new Date(d) : d;
  if (!date || isNaN(date.getTime())) return '';
  return getDateFormatter(opts).format(date);
}

export function formatDateTime(d: DateInput): string {
  return formatDate(d, {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).replace(',', '');
}

export function formatBytes(n?: number | null): string {
  if (n === null || n === undefined || isNaN(n)) return '';
  const units = ['B', 'kB', 'MB', 'GB', 'TB'];
  let value = n;
  let unitIndex = 0;
  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024;
    unitIndex++;
  }
  const fmt = getNumberFormatter({
    maximumFractionDigits: value < 10 ? 1 : 0,
  });
  return `${fmt.format(value)} ${units[unitIndex]}`;
}

export function formatDuration(ms?: number | null): string {
  if (ms === null || ms === undefined || ms < 0) return '';
  const totalSeconds = Math.floor(ms / 1000);
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;
  const parts: string[] = [];
  if (hours) parts.push(`${hours}h`);
  if (minutes) parts.push(`${minutes}m`);
  if (!hours && !minutes) parts.push(`${seconds}s`);
  return parts.join(' ');
}

export function formatBool(v: boolean | null | undefined): string {
  return v ? 'Yes' : 'No';
}

export function formatList(values: string[], max?: number) {
  const limit = max ?? values.length;
  const visible = values.slice(0, limit);
  const hidden = values.length - visible.length;
  return { visible, hidden };
}
