import { Checkbox } from '@/components/ui/checkbox';
import { Button } from '@/components/ui/button';
import { Popover, PopoverTrigger, PopoverContent } from '@/components/ui/popover';

interface Column {
  field: string;
  label: string;
}

interface Props {
  allColumns: Column[];
  visibleColumns: string[];
  setVisibleColumns: (cols: string[]) => void;
}

export default function ColumnSelector({ allColumns, visibleColumns, setVisibleColumns }: Props) {
  const toggleColumn = (field: string, checked: boolean | string) => {
    if (checked) {
      setVisibleColumns([...visibleColumns, field]);
    } else {
      setVisibleColumns(visibleColumns.filter(f => f !== field));
    }
  };

  return (
      <Popover>
        <PopoverTrigger asChild>
          <Button variant="outline">Настроить колонки</Button>
        </PopoverTrigger>
        <PopoverContent className="w-64 space-y-2">
          {allColumns.map(col => (
              <div key={col.field} className="flex items-center space-x-2">
                <Checkbox
                    checked={visibleColumns.includes(col.field)}
                    onCheckedChange={checked => toggleColumn(col.field, checked)}
                />
                <label className="text-sm">{col.label}</label>
              </div>
          ))}
        </PopoverContent>
      </Popover>
  );
}
