import {useForm} from 'react-hook-form';
import {zodResolver} from '@hookform/resolvers/zod';
import {Button} from '@/components/ui/button';
import {Card, CardContent, CardHeader, CardTitle} from '@/components/ui/card';
import {Form} from '@/components/ui/form';
import {type FormData, formSchema} from '@/features/filter/lib/form-schema.ts';
import {FilterFormFields} from "@/components/FilterFormFields.tsx";

export function FilterForm() {
    const form = useForm<FormData>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            searchTerm: '',
            storages: [],
            paths: [],
            persons: [],
            tags: [],
            isRemote: false,
            isFullTime: false,
            isUrgent: false,
            hasExperience: false,
            dateRange: {
                from: undefined,
                to: undefined
            }
        },
    });

    const onSubmit = (data: FormData) => {
        console.log('Form submitted:', data);
    };

    return (
        <Card className="w-full max-w-4xl mx-auto">
            <CardHeader>
                <CardTitle>Advanced Filter Form</CardTitle>
            </CardHeader>
            <CardContent>
                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
                        <FilterFormFields control={form.control}/>
                        {/* Submit Button */}
                        <Button type="submit" className="w-full">
                            Apply Filters
                        </Button>
                    </form>
                </Form>
            </CardContent>
        </Card>
    );
}
