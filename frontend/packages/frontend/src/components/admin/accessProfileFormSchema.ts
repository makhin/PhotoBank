import * as z from 'zod';

export const accessProfileFormSchema = z.object({
  name: z
    .string()
    .min(1, 'Profile name is required')
    .max(128, 'Name must be 128 characters or less'),
  description: z
    .string()
    .max(512, 'Description must be 512 characters or less')
    .optional(),
  flags_CanSeeNsfw: z.boolean(),
  storages: z.array(z.number()).optional(),
  personGroups: z.array(z.number()).optional(),
  dateRanges: z
    .array(
      z.object({
        fromDate: z.date({ required_error: 'From date is required' }),
        toDate: z.date({ required_error: 'To date is required' }),
      })
    )
    .optional(),
});

export type AccessProfileFormValues = z.infer<typeof accessProfileFormSchema>;
