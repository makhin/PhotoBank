import type { Control } from 'react-hook-form';
import { useWatch } from 'react-hook-form';
import { ChevronDownIcon } from 'lucide-react';
import * as React from 'react';
import { addMinutes, format, startOfDay, subMinutes } from 'date-fns';
import { useIsAdmin, useCanSeeNsfw } from '@photobank/shared';
import { useTranslation } from 'react-i18next';

import { useAppSelector } from '@/app/hook';
import { Input } from '@/shared/ui/input';
import { TriStateCheckbox } from '@/shared/ui/tri-state-checkbox';
import { MultiSelect } from '@/shared/ui/multi-select';
import {
    FormControl,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from '@/shared/ui/form';
import type { FormData } from '@/features/filter/lib/form-schema';
import { Popover, PopoverContent, PopoverTrigger } from '@/shared/ui/popover';
import { Button } from '@/shared/ui/button';
import { Calendar } from '@/shared/ui/calendar';


interface FilterFormFieldsProps {
    control: Control<FormData>;
}

const toUtcStartOfDay = (date: Date) =>
    subMinutes(startOfDay(date), date.getTimezoneOffset());

const toLocalFromUtc = (date: Date) => {
    if (
        date.getUTCHours() === 0 &&
        date.getUTCMinutes() === 0 &&
        date.getUTCSeconds() === 0 &&
        date.getUTCMilliseconds() === 0
    ) {
        return addMinutes(date, date.getTimezoneOffset());
    }

    return date;
};

function formatDate(date?: Date | null) {
    if (!date) return '';

    return format(toLocalFromUtc(date), 'dd.MM.yyyy');
}

interface DateFieldProps {
    control: Control<FormData>;
    name: 'dateFrom' | 'dateTo';
    label: string;
    placeholder: string;
    clearLabel: string;
}

const DateField = ({
    control,
    name,
    label,
    placeholder,
    clearLabel,
}: DateFieldProps) => {
    const [open, setOpen] = React.useState(false);

    return (
        <FormField
            control={control}
            name={name}
            render={({ field }) => (
                <FormItem className="flex flex-col">
                    <FormLabel>{label}</FormLabel>
                    <FormControl>
                        <Popover open={open} onOpenChange={setOpen}>
                            <PopoverTrigger asChild>
                                <Button
                                    variant="outline"
                                    id="date"
                                    className="w-48 justify-between font-normal"
                                    onClick={() => {
                                        setOpen(true);
                                    }}
                                >
                                    {field.value ? formatDate(field.value) : placeholder}
                                    <ChevronDownIcon />
                                </Button>
                            </PopoverTrigger>
                            <PopoverContent
                                className="w-auto overflow-hidden p-0"
                                align="start"
                            >
                                <Calendar
                                    mode="single"
                                    selected={
                                        field.value
                                            ? toLocalFromUtc(field.value)
                                            : undefined
                                    }
                                    captionLayout="dropdown"
                                    onSelect={(date) => {
                                        const normalizedDate = date
                                            ? toUtcStartOfDay(date)
                                            : null;
                                        field.onChange(normalizedDate);
                                        setOpen(false);
                                    }}
                                />
                                {field.value ? (
                                    <div className="border-t border-border p-2">
                                        <Button
                                            type="button"
                                            variant="ghost"
                                            className="w-full"
                                            onClick={() => {
                                                field.onChange(null);
                                                setOpen(false);
                                            }}
                                        >
                                            {clearLabel}
                                        </Button>
                                    </div>
                                ) : null}
                            </PopoverContent>
                        </Popover>
                    </FormControl>
                    <FormMessage />
                </FormItem>
            )}
        />
    );
};

export const FilterFormFields = ({ control }: FilterFormFieldsProps) => {
    const isAdmin = useIsAdmin();
    const canSeeNsfw = useCanSeeNsfw();
    const { t } = useTranslation();

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
                                <FormLabel>{t('captionLabel')}</FormLabel>
                                <FormControl>
                                    <Input placeholder={t('captionPlaceholder')} {...field} />
                                </FormControl>
                                <FormMessage/>
                            </FormItem>
                        )}
                    />
                </div>
                {/* Date Range Picker */}
                <div>
                    <DateField
                        control={control}
                        name="dateFrom"
                        label={t('dateFromLabel')}
                        placeholder={t('selectDatePlaceholder')}
                        clearLabel={t('clearDateButton')}
                    />
                </div>

                <div>
                    <DateField
                        control={control}
                        name="dateTo"
                        label={t('dateToLabel')}
                        placeholder={t('selectDatePlaceholder')}
                        clearLabel={t('clearDateButton')}
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
                            <FormLabel>{t('storagesLabel')}</FormLabel>
                            <FormControl>
                                <MultiSelect
                                    value={field.value}
                                    onValueChange={field.onChange}
                                    options={storages}
                                    placeholder={t('selectStoragesPlaceholder')}
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
                            <FormLabel>{t('pathsLabel')}</FormLabel>
                            <FormControl>
                                <MultiSelect
                                    value={field.value}
                                    onValueChange={field.onChange}
                                    options={filteredPaths}
                                    placeholder={t('selectPathsPlaceholder')}
                                    disabled={!selectedStorageIds || selectedStorageIds.length === 0}
                                />
                            </FormControl>
                            <FormMessage/>
                        </FormItem>
                    )}
                />

                <FormField
                    control={control}
                    name="personNames"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>{t('personsLabel')}</FormLabel>
                            <FormControl>
                                <MultiSelect
                                    value={field.value}
                                    onValueChange={field.onChange}
                                    options={persons.map((person) => ({ label: person.name, value: person.name }))}
                                    placeholder={t('selectPersonsPlaceholder')}
                                />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={control}
                    name="tagNames"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>{t('tagsLabel')}</FormLabel>
                            <FormControl>
                                <MultiSelect
                                    value={field.value}
                                    onValueChange={field.onChange}
                                    options={tags.map((tag) => ({ label: tag.name, value: tag.name }))}
                                    placeholder={t('selectTagsPlaceholder')}
                                />
                            </FormControl>
                            <FormMessage />
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
                                <FormLabel>{t('blackWhiteLabel')}</FormLabel>
                            </div>
                        </FormItem>
                    )}
                />

                {(canSeeNsfw || isAdmin) && (
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
                                    <FormLabel>{t('adultContentLabel')}</FormLabel>
                                </div>
                            </FormItem>
                        )}
                    />
                )}

                {(canSeeNsfw || isAdmin) && (
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
                                    <FormLabel>{t('racyContentLabel')}</FormLabel>
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
                                <FormLabel>{t('thisDayLabel')}</FormLabel>
                            </div>
                        </FormItem>
                    )}
                />
            </div>
        </>
    );
};
