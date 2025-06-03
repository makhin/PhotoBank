import { Command, CommandGroup, CommandItem, CommandList } from '@/components/ui/command';
import { Popover, PopoverTrigger, PopoverContent } from '@/components/ui/popover';
import { Button } from '@/components/ui/button';
import { CheckIcon } from '@radix-ui/react-icons';

interface Option {
    label: string;
    value: number;
}

interface MultiSelectProps {
    selected: number[];
    onChange: (values: number[]) => void;
    options: Option[];
    placeholder?: string;
}

export default function MultiSelect({ selected, onChange, options, placeholder }: MultiSelectProps) {
    const toggle = (value: number) => {
        onChange(
            selected.includes(value)
                ? selected.filter(v => v !== value)
                : [...selected, value]
        );
    };

    return (
        <Popover>
            <PopoverTrigger asChild>
                <Button variant="outline" className="w-full justify-start">
                    {selected.length > 0
                        ? options.filter(o => selected.includes(o.value)).map(o => o.label).join(', ')
                        : placeholder || 'Выберите...'}
                </Button>
            </PopoverTrigger>
            <PopoverContent className="w-[200px] p-0">
                <Command>
                    <CommandList>
                        <CommandGroup>
                            {options.map(option => (
                                <CommandItem
                                    key={option.value}
                                    onSelect={() => toggle(option.value)}
                                    className="cursor-pointer"
                                >
                  <span className="mr-2">
                    {selected.includes(option.value) ? <CheckIcon className="w-4 h-4" /> : null}
                  </span>
                                    {option.label}
                                </CommandItem>
                            ))}
                        </CommandGroup>
                    </CommandList>
                </Command>
            </PopoverContent>
        </Popover>
    );
}
