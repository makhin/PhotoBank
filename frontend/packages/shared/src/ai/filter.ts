import { z } from "zod";

export const PhotoFilterSchema = z.object({
  persons: z.array(z.string()).default([]),
  tags: z.array(z.string()).default([]),
  dateFrom: z.string().pipe(z.iso.datetime({ offset: false })).nullable().default(null), // "YYYY-MM-DD"
  dateTo: z.string().pipe(z.iso.datetime({ offset: false })).nullable().default(null),
});
export type PhotoFilter = z.infer<typeof PhotoFilterSchema>;
