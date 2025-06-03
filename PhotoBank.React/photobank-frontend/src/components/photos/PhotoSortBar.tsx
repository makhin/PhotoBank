import { Button } from '@/components/ui/button';
import { ArrowDownIcon, ArrowUpIcon } from '@radix-ui/react-icons';

interface Props {
  sortField: string;
  sortDirection: 'asc' | 'desc';
  setSortField: (field: string) => void;
  setSortDirection: (dir: 'asc' | 'desc') => void;
}

const fields = [
  { value: 'takenDate', label: 'Дата' },
  { value: 'name', label: 'Имя' },
  { value: 'id', label: 'ID' },
];

export default function PhotoSortBar({ sortField, sortDirection, setSortField, setSortDirection }: Props) {
  return (
      <div className="flex items-center gap-2">
        <span className="text-sm">Сортировка:</span>
        {fields.map(({ value, label }) => (
            <Button
                key={value}
                variant={sortField === value ? 'default' : 'outline'}
                size="sm"
                onClick={() => {
                  if (sortField === value) {
                    setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
                  } else {
                    setSortField(value);
                    setSortDirection('asc');
                  }
                }}
            >
              {label}
              {sortField === value && (
                  sortDirection === 'asc' ? <ArrowUpIcon className="ml-1 w-4 h-4" /> : <ArrowDownIcon className="ml-1 w-4 h-4" />
              )}
            </Button>
        ))}
      </div>
  );
}
