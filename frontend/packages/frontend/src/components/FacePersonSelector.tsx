import {useState} from "react";
import type { PersonDto } from '@photobank/shared/api/photobank';
import {
    unassignedLabel,
    facePrefix,
    searchPersonPlaceholder,
    noPersonFoundText,
    noneLabel,
} from '@photobank/shared/constants';

import {
    Popover,
    PopoverTrigger,
    PopoverContent,
} from "@/components/ui/popover";
import {Button} from "@/components/ui/button";
import {
    Command,
    CommandInput,
    CommandEmpty,
    CommandGroup,
    CommandItem,
} from "@/components/ui/command";


interface FacePersonSelectorProps {
    faceIndex: number;
    personId?: number;
    persons: PersonDto[];
    disabled?: boolean;
    onChange?: (personId: number | undefined) => void;
}

export const FacePersonSelector = ({
                                       faceIndex,
                                       personId,
                                       persons,
                                       disabled = false,
                                       onChange,
                                   }: FacePersonSelectorProps) => {
    const [open, setOpen] = useState(false);
    const [selectedId, setSelectedId] = useState<number | undefined>(personId);

    const selectedName =
        persons.find((p) => p.id === selectedId)?.name ?? unassignedLabel;

    const handleSelect = (id: number | undefined) => {
        setSelectedId(id);
        onChange?.(id);
        setOpen(false);
    };

    return (
        <div className="flex flex-col gap-1">
            <div className="flex items-center gap-2">
                <span className="font-medium">{facePrefix} {faceIndex + 1}:</span>
                <Popover open={open} onOpenChange={setOpen}>
                    <PopoverTrigger asChild>
                        <Button
                            variant="outline"
                            className="w-[200px] justify-start text-left text-sm"
                            disabled={disabled}
                        >
                            {selectedName}
                        </Button>
                    </PopoverTrigger>
                    <PopoverContent className="w-[200px] p-0">
                        <Command>
                            <CommandInput placeholder={searchPersonPlaceholder}/>
                            <CommandEmpty>{noPersonFoundText}</CommandEmpty>
                            <CommandGroup>
                                <CommandItem onSelect={() => { handleSelect(undefined); }}>
                                    {noneLabel}
                                </CommandItem>
                                {persons.map((person) => (
                                    <CommandItem
                                        key={person.id}
                                        value={person.name}
                                        onSelect={() => { handleSelect(person.id); }}
                                    >
                                        {person.name}
                                    </CommandItem>
                                ))}
                            </CommandGroup>
                        </Command>
                    </PopoverContent>
                </Popover>
            </div>
        </div>
    );
};
