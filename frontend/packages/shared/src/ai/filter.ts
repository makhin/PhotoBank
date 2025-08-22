import { z } from "zod";

export const PhotoFilterSchema = z.object({
  personNames: z.array(z.string()).default([]),
  tagNames: z.array(z.string()).default([]),
  dateFrom: z
    .string()
    .nullable()
    .default(null)
    .transform((val) => {
      if (!val) return null;
      const iso = val.includes('T') ? val : `${val}T00:00:00Z`;
      return new Date(iso);
    }),

  dateTo: z
    .string()
    .nullable()
    .default(null)
    .transform((val) => {
      if (!val) return null;
      const iso = val.includes('T') ? val : `${val}T00:00:00Z`;
      return new Date(iso);
    }),
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
