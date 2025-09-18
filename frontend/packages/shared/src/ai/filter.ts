import { isValid, parseISO } from 'date-fns';
import { z } from "zod";

const toFilterDate = (value: string | null): Date | null => {
  if (!value) return null;

  const iso = value.includes('T') ? value : `${value}T00:00:00Z`;
  const parsed = parseISO(iso);

  if (!isValid(parsed)) {
    return null;
  }

  return parsed;
};

export const PhotoFilterSchema = z.object({
  personNames: z.array(z.string()).default([]),
  tagNames: z.array(z.string()).default([]),
  dateFrom: z
    .string()
    .nullable()
    .default(null)
    .transform((val) => toFilterDate(val)),

  dateTo: z
    .string()
    .nullable()
    .default(null)
    .transform((val) => toFilterDate(val)),
});

export const photoFilterSchemaForLLM = {
  type: 'object',
  properties: {
    personNames: {
      type: 'array',
      items: { type: 'string' },
    },
    tagNames: {
      type: 'array',
      items: { type: 'string' },
    },
    dateFrom: { type: 'string' },
    dateTo: { type: 'string' },
  },
  required: ['personNames', 'tagNames', 'dateFrom', 'dateTo'],
  additionalProperties: false,
};

export type PhotoFilter = z.infer<typeof PhotoFilterSchema>;
