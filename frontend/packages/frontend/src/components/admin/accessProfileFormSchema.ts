import * as z from 'zod';

const nullableDate = z.coerce.date({ error: 'Date must be a valid date' }).nullish();

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
        fromDate: nullableDate,
        toDate: nullableDate,
      })
    )
    .optional(),
});

export type AccessProfileFormValues = z.infer<typeof accessProfileFormSchema>;
export type AccessProfileFormInput = z.input<typeof accessProfileFormSchema>;
