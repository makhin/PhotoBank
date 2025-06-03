import { useMemo } from 'react';
import { useFormContext, Controller, useWatch } from 'react-hook-form';
import MultiSelect from '@/components/ui/multiselect';
import { Input } from '@/components/ui/input';
import { Calendar } from '@/components/ui/calendar';
import { Popover, PopoverTrigger, PopoverContent } from '@/components/ui/popover';
import { Button } from '@/components/ui/button';
import { useAppSelector } from '@/app/hooks';
import { format } from 'date-fns';

type Props = {
  onSubmit: () => void;
  onReset: () => void;
};

export default function PhotoFilters({ onSubmit, onReset }: Props) {
  const { control, setValue, handleSubmit } = useFormContext();
  const tags = useAppSelector(state => state.meta.tags);
  const persons = useAppSelector(state => state.meta.persons);
  const storages = useAppSelector(state => state.meta.storages);
  const paths = useAppSelector(state => state.meta.paths);
  const dateFrom = useWatch({ control, name: 'takenDateFrom' });
  const dateTo = useWatch({ control, name: 'takenDateTo' });
  const selectedStorages = useWatch({ control, name: 'storages' });

  const dateRange = useMemo(() => [dateFrom ? new Date(dateFrom) : undefined, dateTo ? new Date(dateTo) : undefined], [dateFrom, dateTo]);

  const filteredPaths = useMemo(() => {
    if (!selectedStorages?.length) return [];
    return paths.filter(p => selectedStorages.includes(p.storageId));
  }, [paths, selectedStorages]);

  return (
      <form onSubmit={handleSubmit(onSubmit)} className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <Controller name="storages" control={control} render={({ field }) => (
            <MultiSelect placeholder="Выбери хранилище" selected={field.value} onChange={field.onChange} options={storages.map(s => ({ label: s.name ?? '', value: s.id }))} />)} />

        <Controller name="paths" control={control} render={({ field }) => (
            <MultiSelect placeholder="Выбери папки" selected={field.value} onChange={field.onChange} options={filteredPaths.map(p => ({ label: p.path ?? '', value: p.storageId }))} />)} />

        <Popover>
          <PopoverTrigger asChild>
            <Button variant="outline" className="justify-start text-left col-span-full">
              {dateRange[0] && dateRange[1] ? `${format(dateRange[0], 'dd.MM.yyyy')} - ${format(dateRange[1], 'dd.MM.yyyy')}` : 'Выбери диапазон дат'}
            </Button>
          </PopoverTrigger>
          <PopoverContent className="w-auto p-0">
            <Calendar
                initialFocus
                mode="range"
                selected={{ from: dateRange[0], to: dateRange[1] }}
                onSelect={(range) => {
                  setValue('takenDateFrom', range?.from?.toISOString() ?? '');
                  setValue('takenDateTo', range?.to?.toISOString() ?? '');
                }}
                numberOfMonths={2}
            />
          </PopoverContent>
        </Popover>

        <Controller name="caption" control={control} render={({ field }) => <Input placeholder="Подпись" {...field} />} />
        <Controller name="relativePath" control={control} render={({ field }) => <Input placeholder="Путь" {...field} />} />

        <Controller name="tags" control={control} render={({ field }) => (
            <MultiSelect placeholder="Выбери теги" selected={field.value} onChange={field.onChange} options={tags.map(tag => ({ label: tag.name ?? '', value: tag.id }))} />)} />

        <Controller name="persons" control={control} render={({ field }) => (
            <MultiSelect placeholder="Выбери людей" selected={field.value} onChange={field.onChange} options={persons.map(person => ({ label: person.name ?? '', value: person.id }))} />)} />

        <Controller name="isBW" control={control} render={({ field }) => (
            <label className="flex items-center gap-2">
              <input type="checkbox" {...field} /> Ч/Б
            </label>)} />

        <Controller name="isAdultContent" control={control} render={({ field }) => (
            <label className="flex items-center gap-2">
              <input type="checkbox" {...field} /> Контент 18+
            </label>)} />

        <Controller name="isRacyContent" control={control} render={({ field }) => (
            <label className="flex items-center gap-2">
              <input type="checkbox" {...field} /> Racy
            </label>)} />

        <Controller name="thisDay" control={control} render={({ field }) => (
            <label className="flex items-center gap-2">
              <input type="checkbox" {...field} /> Только "этот день"
            </label>)} />

        <div className="col-span-full flex gap-4">
          <Button type="submit">Применить фильтр</Button>
          <Button type="button" variant="secondary" onClick={onReset}>Сбросить</Button>
        </div>
      </form>
  );
}
