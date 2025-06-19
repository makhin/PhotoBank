import {z} from 'zod';

export const formSchema = z.object({
    searchTerm: z.string().optional(),
    storages: z.array(z.string()).optional(),
    paths: z.array(z.string()).optional(),
    persons: z.array(z.string()).optional(),
    tags: z.array(z.string()).optional(),
    isRemote: z.boolean().optional(),
    isFullTime: z.boolean().optional(),
    isUrgent: z.boolean().optional(),
    hasExperience: z.boolean().optional(),
    dateRange: z.object({
        from: z.date().optional(),
        to: z.date().optional(),
    }).optional(),
});

export type FormData = z.infer<typeof formSchema>;