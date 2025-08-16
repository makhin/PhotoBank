import { Buffer } from 'buffer';
import type { FormData } from '@/features/filter/lib/form-schema';
import { formSchema } from '@/features/filter/lib/form-schema';

const toBase64 = (json: string) =>
  typeof window === 'undefined'
    ? Buffer.from(json, 'utf-8').toString('base64')
    : window.btoa(json);

const fromBase64 = (encoded: string) =>
  typeof window === 'undefined'
    ? Buffer.from(encoded, 'base64').toString('utf-8')
    : window.atob(encoded);

export const serializeFilter = (data: FormData): string => {
  const json = JSON.stringify(data);
  return toBase64(json);
};

export const deserializeFilter = (value: string): FormData | null => {
  try {
    const json = fromBase64(value);
    const parsed = formSchema.parse(JSON.parse(json));
    return parsed;
  } catch {
    return null;
  }
};
