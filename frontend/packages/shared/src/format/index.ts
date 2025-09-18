import { format } from 'date-fns';

import {
  DEFAULT_DATE_FORMAT,
  DEFAULT_DATE_TIME_FORMAT,
  toDate,
  type FlexibleDateInput,
} from '../utils/parseDate';

export type DateInput = FlexibleDateInput;

const locale =
  typeof navigator !== 'undefined' && navigator.language
    ? navigator.language
    : 'en-US';

const numberFormatterCache = new Map<string, Intl.NumberFormat>();

function getNumberFormatter(opts?: Intl.NumberFormatOptions) {
  const key = JSON.stringify(opts ?? {});
  let fmt = numberFormatterCache.get(key);
  if (!fmt) {
    fmt = new Intl.NumberFormat(locale, opts);
    numberFormatterCache.set(key, fmt);
  }
  return fmt;
}

export function formatDate(d: DateInput): string {
  const date = toDate(d);
  if (!date) return '';
  return format(date, DEFAULT_DATE_FORMAT);
}

export function formatDateTime(d: DateInput): string {
  const date = toDate(d);
  if (!date) return '';
  return format(date, DEFAULT_DATE_TIME_FORMAT);
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
