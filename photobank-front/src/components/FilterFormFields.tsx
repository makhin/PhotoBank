import type {Control} from 'react-hook-form';
import {useWatch} from 'react-hook-form';
import {useSelector} from 'react-redux';
import {ChevronDownIcon} from 'lucide-react';
import * as React from "react";
import {format} from 'date-fns';

import {Input} from '@/components/ui/input';
import {Checkbox} from '@/components/ui/checkbox';
import {MultiSelect} from '@/components/ui/multi-select';
import {FormControl, FormField, FormItem, FormLabel, FormMessage,} from '@/components/ui/form';
import type {FormData} from '@/features/filter/lib/form-schema.ts';
import type {RootState} from '@/app/store.ts';
import {Popover, PopoverContent, PopoverTrigger} from "@/components/ui/popover.tsx";

import {Button} from './ui/button';
import {Calendar} from './ui/calendar';

interface FilterFormFieldsProps {
    control: Control<FormData>;
}

export const FilterFormFields = ({control}: FilterFormFieldsProps) => {
    const [openFrom, setOpenFrom] = React.useState(false)
    const [openTo, setOpenTo] = React.useState(false)

    const tags = useSelector((state: RootState) => state.metadata.tags)
    const persons = useSelector((state: RootState) => state.metadata.persons)

    const selectedStorageIds = useWatch({
        control,
        name: 'storages',
    });

    const storages = useSelector(
        (state: RootState) => state.metadata.storages
    ).map((s) => {
        return {label: s.name, value: s.id.toString()};
    });

    const filteredPaths = useSelector((state: RootState) => state.metadata.paths)
        .filter((p) =>
            !selectedStorageIds || selectedStorageIds.length === 0
                ? true
                : selectedStorageIds.includes(p.storageId.toString())
        )
        .map((s) => ({
            label: s.path,
            value: s.path,
        }));

    function formatDate(date?: Date) {
        return date ? format(date, 'dd.MM.yyyy') : '';
    }

    return (
        <>
            {/* Search Input */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                <div className="sm:col-span-2">
                    <FormField
                        control={control}
                        name="caption"
                        render={({field}) => (
                            <FormItem className="flex flex-col">
                                <FormLabel>Caption</FormLabel>
                                <FormControl>
                                    <Input placeholder="Enter caption..." {...field} />
                                </FormControl>
                                <FormMessage/>
                            </FormItem>
                        )}
                    />
                </div>
                {/* Date Range Picker */}
                <div>
                    <FormField
                        control={control}
                        name="dateFrom"
                        render={({field}) => (
                            <FormItem className="flex flex-col">
                                <FormLabel>Date From</FormLabel>
                                <FormControl>
                                    <Popover open={openFrom} onOpenChange={setOpenFrom}>
                                        <PopoverTrigger asChild>
                                            <Button
                                                variant="outline"
                                                id="date"
                                                className="w-48 justify-between font-normal"
                                                onClick={() => { setOpenFrom(true); }}
                                            >
                                                {field.value ? formatDate(field.value) : "Select date"}
                                                <ChevronDownIcon/>
                                            </Button>
                                        </PopoverTrigger>
                                        <PopoverContent className="w-auto overflow-hidden p-0" align="start">
                                            <Calendar
                                                mode="single"
                                                selected={field.value}
                                                captionLayout="dropdown"
                                                onSelect={(d) => {
                                                    field.value = d;
                                                    setOpenFrom(false);
                                                }}
                                            />
                                        </PopoverContent>
                                    </Popover>
                                </FormControl>
                                <FormMessage/>
                            </FormItem>
                        )}
                    />
                </div>

                {/* Date Range Picker */}
                <div>
                    <FormField
                        control={control}
                        name="dateTo"
                        render={({field}) => (
                            <FormItem className="flex flex-col">
                                <FormLabel>Date To</FormLabel>
                                <FormControl>
                                    <Popover open={openTo} onOpenChange={setOpenTo}>
                                        <PopoverTrigger asChild>
                                            <Button
                                                variant="outline"
                                                id="date"
                                                className="w-48 justify-between font-normal"
                                                onClick={() => { setOpenTo(true); }}
                                            >
                                                {field.value ? formatDate(field.value) : "Select date"}
                                                <ChevronDownIcon/>
                                            </Button>
                                        </PopoverTrigger>
                                        <PopoverContent className="w-auto overflow-hidden p-0" align="start">
                                            <Calendar
                                                mode="single"
                                                selected={field.value}
                                                captionLayout="dropdown"
                                                onSelect={(d) => {
                                                    field.value = d;
                                                    setOpenTo(false);
                                                }}
                                            />
                                        </PopoverContent>
                                    </Popover>
                                </FormControl>
                                <FormMessage/>
                            </FormItem>
                        )}
                    />
                </div>
            </div>
            {/* Multiselects Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <FormField
                    control={control}
                    name="storages"
                    render={({field}) => (
                        <FormItem>
                            <FormLabel>Storages</FormLabel>
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
                            <FormLabel>Paths</FormLabel>
                            <FormControl>
                                <MultiSelect
                                    value={field.value}
                                    onValueChange={field.onChange}
                                    options={filteredPaths}
                                    placeholder="Select paths"
                                    disabled={!selectedStorageIds || selectedStorageIds.length === 0}
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
                            <FormLabel>Persons</FormLabel>
                            <FormControl>
                                <MultiSelect
                                    value={field.value}
                                    onValueChange={field.onChange}
                                    options={persons.map(person => ({ label: person.name, value: person.id.toString() }))}
                                    placeholder="Select persons"
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
                            <FormLabel>Tags</FormLabel>
                            <FormControl>
                                <MultiSelect
                                    value={field.value}
                                    onValueChange={field.onChange}
                                    options={tags.map(tag => ({ label: tag.name, value: tag.id.toString() }))}
                                    placeholder="Select tags"
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
                    name="isBW"
                    render={({field}) => (
                        <FormItem className="flex flex-row items-start space-x-3 space-y-0">
                            <FormControl>
                                <Checkbox
                                    checked={field.value}
                                    onCheckedChange={field.onChange}
                                />
                            </FormControl>
                            <div className="space-y-1 leading-none">
                                <FormLabel>Black-White</FormLabel>
                            </div>
                        </FormItem>
                    )}
                />

                <FormField
                    control={control}
                    name="isAdultContent"
                    render={({field}) => (
                        <FormItem className="flex flex-row items-start space-x-3 space-y-0">
                            <FormControl>
                                <Checkbox
                                    checked={field.value}
                                    onCheckedChange={field.onChange}
                                />
                            </FormControl>
                            <div className="space-y-1 leading-none">
                                <FormLabel>Adult Content</FormLabel>
                            </div>
                        </FormItem>
                    )}
                />

                <FormField
                    control={control}
                    name="isRacyContent"
                    render={({field}) => (
                        <FormItem className="flex flex-row items-start space-x-3 space-y-0">
                            <FormControl>
                                <Checkbox
                                    checked={field.value}
                                    onCheckedChange={field.onChange}
                                />
                            </FormControl>
                            <div className="space-y-1 leading-none">
                                <FormLabel>Racy Content</FormLabel>
                            </div>
                        </FormItem>
                    )}
                />

                <FormField
                    control={control}
                    name="thisDay"
                    render={({field}) => (
                        <FormItem className="flex flex-row items-start space-x-3 space-y-0">
                            <FormControl>
                                <Checkbox
                                    checked={field.value}
                                    onCheckedChange={field.onChange}
                                />
                            </FormControl>
                            <div className="space-y-1 leading-none">
                                <FormLabel>This Day</FormLabel>
                            </div>
                        </FormItem>
                    )}
                />
            </div>
        </>
    );
};
