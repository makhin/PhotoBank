import { z } from 'zod';

export const formSchema = z.object({
  caption: z.string().optional(),
  storages: z.array(z.string()).optional(),
  paths: z.array(z.string()).optional(),
  persons: z.array(z.string()).optional(),
  tags: z.array(z.string()).optional(),
  isBW: z.boolean().optional(),
  isRacyContent: z.boolean().optional(),
  isAdultContent: z.boolean().optional(),
  thisDay: z.boolean().optional(),
  dateFrom: z.date().optional(),
  dateTo: z.date().optional(),
});

export type FormData = z.infer<typeof formSchema>;
