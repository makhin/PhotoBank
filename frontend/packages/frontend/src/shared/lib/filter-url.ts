import { formatISO, parseISO } from 'date-fns';

import { formSchema, type FormData } from '@/features/filter/lib/form-schema';

const toBase64 = (json: string) => {
  if (typeof window === 'undefined') {
    // SSR: используем TextEncoder/Uint8Array → base64
    const bytes = new TextEncoder().encode(json);
    let bin = '';
    bytes.forEach((b) => (bin += String.fromCharCode(b)));
    return Buffer.from(bin, 'binary').toString('base64');
  }
  return window.btoa(unescape(encodeURIComponent(json)));
};

const fromBase64 = (encoded: string) => {
  if (typeof window === 'undefined') {
    const bin = Buffer.from(encoded, 'base64').toString('binary');
    const bytes = Uint8Array.from(bin, (c) => c.charCodeAt(0));
    return new TextDecoder().decode(bytes);
  }
  return decodeURIComponent(escape(window.atob(encoded)));
};

const ensureDate = (value: unknown): Date | undefined => {
  if (!value) return undefined;
  if (value instanceof Date) return value;
  if (typeof value === 'string') {
    const parsedDate = parseISO(value);
    return Number.isNaN(parsedDate.getTime()) ? undefined : parsedDate;
  }
  return undefined;
};

export const serializeFilter = (data: FormData): string => {
  const json = JSON.stringify({
    ...data,
    dateFrom: data.dateFrom ? formatISO(data.dateFrom) : undefined,
    dateTo: data.dateTo ? formatISO(data.dateTo) : undefined,
  });
  return toBase64(json);
};

export const deserializeFilter = (value: string): FormData | null => {
  try {
    const json = fromBase64(value);
    const raw = JSON.parse(json) as Record<string, unknown>;
    const parsed = formSchema.parse({
      ...raw,
      dateFrom: ensureDate(raw.dateFrom),
      dateTo: ensureDate(raw.dateTo),
    });
    return parsed;
  } catch {
    return null;
  }
};
