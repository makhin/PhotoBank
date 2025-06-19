import type {Control} from 'react-hook-form';
import {Input} from '@/components/ui/input';
import {Checkbox} from '@/components/ui/checkbox';
import {MultiSelect} from '@/components/ui/multi-select';
import {DatePickerWithRange} from '@/components/ui/date-picker-range';
import {FormControl, FormField, FormItem, FormLabel, FormMessage,} from '@/components/ui/form';
import type {FormData} from '@/features/filter/lib/form-schema.ts';
import {useSelector} from "react-redux";
import type {RootState} from "@/app/store.ts";

interface FilterFormFieldsProps {
    control: Control<FormData>;
}

export const FilterFormFields = ({control}: FilterFormFieldsProps) => {
    const storages = useSelector((state: RootState) => state.metadata.storages).map(s => {
        return {label: s.name, value: s.id.toString()};
    });
    const paths = useSelector((state: RootState) => state.metadata.paths).map(s => {
        return {label: s.path, value: s.path};
    });
    const persons = useSelector((state: RootState) => state.metadata.persons).map(s => {
        return {label: s.name, value: s.id.toString()};
    });
    const tags = useSelector((state: RootState) => state.metadata.tags).map(s => {
        return {label: s.name, value: s.id.toString()};
    });

    return (
        <>
            {/* Search Input */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <FormField
                    control={control}
                    name="searchTerm"
                    render={({field}) => (
                        <FormItem>
                            <FormLabel>Search Term</FormLabel>
                            <FormControl>
                                <Input placeholder="Enter search term..." {...field} />
                            </FormControl>
                            <FormMessage/>
                        </FormItem>
                    )}
                />

                {/* Date Range Picker */}
                <FormField
                    control={control}
                    name="dateRange"
                    render={({field}) => (
                        <FormItem className="flex flex-col">
                            <FormLabel>Date Range</FormLabel>
                            <FormControl>
                                <DatePickerWithRange {...field} />
                            </FormControl>
                            <FormMessage/>
                        </FormItem>
                    )}
                />
            </div>
            {/* Multiselects Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <FormField
                    control={control}
                    name="storages"
                    render={({field}) => (
                        <FormItem>
                            <FormLabel>Categories</FormLabel>
                            <FormControl>
                                <MultiSelect
                                    value={field.value}
                                    onValueChange={field.onChange}
                                    options={storages}
                                    placeholder="Select storages"
                                />
                            </FormControl>
                            <FormMessage/>
                        </FormItem>
                    )}
                />

                <FormField
                    control={control}
                    name="paths"
                    render={({field}) => (
                        <FormItem>
                            <FormLabel>Skills</FormLabel>
                            <FormControl>
                                <MultiSelect
                                    value={field.value}
                                    onValueChange={field.onChange}
                                    options={paths}
                                    placeholder="Select skills"
                                />
                            </FormControl>
                            <FormMessage/>
                        </FormItem>
                    )}
                />

                <FormField
                    control={control}
                    name="persons"
                    render={({field}) => (
                        <FormItem>
                            <FormLabel>Locations</FormLabel>
                            <FormControl>
                                <MultiSelect
                                    value={field.value}
                                    onValueChange={field.onChange}
                                    options={persons}
                                    placeholder="Select locations"
                                />
                            </FormControl>
                            <FormMessage/>
                        </FormItem>
                    )}
                />

                <FormField
                    control={control}
                    name="tags"
                    render={({field}) => (
                        <FormItem>
                            <FormLabel>Departments</FormLabel>
                            <FormControl>
                                <MultiSelect
                                    value={field.value}
                                    onValueChange={field.onChange}
                                    options={tags}
                                    placeholder="Select departments"
                                />
                            </FormControl>
                            <FormMessage/>
                        </FormItem>
                    )}
                />
            </div>

            {/* Checkboxes */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                <FormField
                    control={control}
                    name="isRemote"
                    render={({field}) => (
                        <FormItem className="flex flex-row items-start space-x-3 space-y-0">
                            <FormControl>
                                <Checkbox
                                    checked={field.value}
                                    onCheckedChange={field.onChange}
                                />
                            </FormControl>
                            <div className="space-y-1 leading-none">
                                <FormLabel>Remote Work</FormLabel>
                            </div>
                        </FormItem>
                    )}
                />

                <FormField
                    control={control}
                    name="isFullTime"
                    render={({field}) => (
                        <FormItem className="flex flex-row items-start space-x-3 space-y-0">
                            <FormControl>
                                <Checkbox
                                    checked={field.value}
                                    onCheckedChange={field.onChange}
                                />
                            </FormControl>
                            <div className="space-y-1 leading-none">
                                <FormLabel>Full Time</FormLabel>
                            </div>
                        </FormItem>
                    )}
                />

                <FormField
                    control={control}
                    name="isUrgent"
                    render={({field}) => (
                        <FormItem className="flex flex-row items-start space-x-3 space-y-0">
                            <FormControl>
                                <Checkbox
                                    checked={field.value}
                                    onCheckedChange={field.onChange}
                                />
                            </FormControl>
                            <div className="space-y-1 leading-none">
                                <FormLabel>Urgent</FormLabel>
                            </div>
                        </FormItem>
                    )}
                />

                <FormField
                    control={control}
                    name="hasExperience"
                    render={({field}) => (
                        <FormItem className="flex flex-row items-start space-x-3 space-y-0">
                            <FormControl>
                                <Checkbox
                                    checked={field.value}
                                    onCheckedChange={field.onChange}
                                />
                            </FormControl>
                            <div className="space-y-1 leading-none">
                                <FormLabel>Experience Required</FormLabel>
                            </div>
                        </FormItem>
                    )}
                />
            </div>
        </>
    );
};
