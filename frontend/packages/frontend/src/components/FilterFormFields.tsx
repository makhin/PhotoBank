import type {Control} from 'react-hook-form';
import {useWatch} from 'react-hook-form';
import {ChevronDownIcon} from 'lucide-react';
import * as React from "react";
import {format} from 'date-fns';
import { useIsAdmin } from '@photobank/shared';
import {
    captionLabel,
    captionPlaceholder,
    dateFromLabel,
    selectDatePlaceholder,
    dateToLabel,
    storagesLabel,
    selectStoragesPlaceholder,
    pathsLabel,
    selectPathsPlaceholder,
    personsLabel,
    selectPersonsPlaceholder,
    tagsLabel,
    selectTagsPlaceholder,
    blackWhiteLabel,
    adultContentLabel,
    racyContentLabel,
    thisDayLabel,
} from '@photobank/shared/constants';

import { useAppSelector } from '@/app/hook';
import {Input} from '@/shared/ui/input';
import {TriStateCheckbox} from '@/shared/ui/tri-state-checkbox';
import {MultiSelect} from '@/shared/ui/multi-select';
import {FormControl, FormField, FormItem, FormLabel, FormMessage,} from '@/shared/ui/form';
import type {FormData} from '@/features/filter/lib/form-schema';
import {Popover, PopoverContent, PopoverTrigger} from "@/shared/ui/popover";
import {Button} from '@/shared/ui/button';
import {Calendar} from '@/shared/ui/calendar';


interface FilterFormFieldsProps {
    control: Control<FormData>;
}

export const FilterFormFields = ({control}: FilterFormFieldsProps) => {
    const [openFrom, setOpenFrom] = React.useState(false)
    const [openTo, setOpenTo] = React.useState(false)
    const isAdmin = useIsAdmin()

    const tags = useAppSelector((state) => state.metadata.tags)
    const persons = useAppSelector((state) => state.metadata.persons)

    const selectedStorageIds = useWatch({
        control,
        name: 'storages',
    });

    const storages = useAppSelector(
        (state) => state.metadata.storages
    ).map((s) => {
        return {label: s.name, value: s.id.toString()};
    });

    const filteredPaths = useAppSelector((state) => state.metadata.paths)
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
                                <FormLabel>{captionLabel}</FormLabel>
                                <FormControl>
                                    <Input placeholder={captionPlaceholder} {...field} />
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
                                <FormLabel>{dateFromLabel}</FormLabel>
                                <FormControl>
                                    <Popover open={openFrom} onOpenChange={setOpenFrom}>
                                        <PopoverTrigger asChild>
                                            <Button
                                                variant="outline"
                                                id="date"
                                                className="w-48 justify-between font-normal"
                                                onClick={() => { setOpenFrom(true); }}
                                            >
                                                {field.value ? formatDate(field.value) : selectDatePlaceholder}
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
                                <FormLabel>{dateToLabel}</FormLabel>
                                <FormControl>
                                    <Popover open={openTo} onOpenChange={setOpenTo}>
                                        <PopoverTrigger asChild>
                                            <Button
                                                variant="outline"
                                                id="date"
                                                className="w-48 justify-between font-normal"
                                                onClick={() => { setOpenTo(true); }}
                                            >
                                                {field.value ? formatDate(field.value) : selectDatePlaceholder}
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
                            <FormLabel>{storagesLabel}</FormLabel>
                            <FormControl>
                                <MultiSelect
                                    value={field.value}
                                    onValueChange={field.onChange}
                                    options={storages}
                                    placeholder={selectStoragesPlaceholder}
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
                            <FormLabel>{pathsLabel}</FormLabel>
                            <FormControl>
                                <MultiSelect
                                    value={field.value}
                                    onValueChange={field.onChange}
                                    options={filteredPaths}
                                    placeholder={selectPathsPlaceholder}
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
                            <FormLabel>{personsLabel}</FormLabel>
                            <FormControl>
                                <MultiSelect
                                    value={field.value}
                                    onValueChange={field.onChange}
                                    options={persons.map(person => ({ label: person.name, value: person.id.toString() }))}
                                    placeholder={selectPersonsPlaceholder}
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
                            <FormLabel>{tagsLabel}</FormLabel>
                            <FormControl>
                                <MultiSelect
                                    value={field.value}
                                    onValueChange={field.onChange}
                                    options={tags.map(tag => ({ label: tag.name, value: tag.id.toString() }))}
                                    placeholder={selectTagsPlaceholder}
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
                                <TriStateCheckbox
                                    value={field.value}
                                    onValueChange={field.onChange}
                                />
                            </FormControl>
                            <div className="space-y-1 leading-none">
                                <FormLabel>{blackWhiteLabel}</FormLabel>
                            </div>
                        </FormItem>
                    )}
                />

                {isAdmin && (
                    <FormField
                        control={control}
                        name="isAdultContent"
                        render={({field}) => (
                            <FormItem className="flex flex-row items-start space-x-3 space-y-0">
                                <FormControl>
                                    <TriStateCheckbox
                                        value={field.value}
                                        onValueChange={field.onChange}
                                    />
                                </FormControl>
                                <div className="space-y-1 leading-none">
                                    <FormLabel>{adultContentLabel}</FormLabel>
                                </div>
                            </FormItem>
                        )}
                    />
                )}

                {isAdmin && (
                    <FormField
                        control={control}
                        name="isRacyContent"
                        render={({field}) => (
                            <FormItem className="flex flex-row items-start space-x-3 space-y-0">
                                <FormControl>
                                    <TriStateCheckbox
                                        value={field.value}
                                        onValueChange={field.onChange}
                                    />
                                </FormControl>
                                <div className="space-y-1 leading-none">
                                    <FormLabel>{racyContentLabel}</FormLabel>
                                </div>
                            </FormItem>
                        )}
                    />
                )}

                <FormField
                    control={control}
                    name="thisDay"
                    render={({field}) => (
                        <FormItem className="flex flex-row items-start space-x-3 space-y-0">
                            <FormControl>
                                <TriStateCheckbox
                                    value={field.value}
                                    onValueChange={field.onChange}
                                />
                            </FormControl>
                            <div className="space-y-1 leading-none">
                                <FormLabel>{thisDayLabel}</FormLabel>
                            </div>
                        </FormItem>
                    )}
                />
            </div>
        </>
    );
};
