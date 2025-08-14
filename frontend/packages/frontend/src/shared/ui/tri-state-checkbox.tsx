"use client"

import * as React from "react"
import * as CheckboxPrimitive from "@radix-ui/react-checkbox"
import { CheckIcon, MinusIcon } from "lucide-react"

import { cn } from "@/shared/lib/utils"

interface TriStateCheckboxProps extends Omit<
  React.ComponentPropsWithoutRef<typeof CheckboxPrimitive.Root>,
  "checked" | "onCheckedChange" | "value"
> {
  value: CheckboxPrimitive.CheckedState | undefined
  onValueChange: (value: CheckboxPrimitive.CheckedState | undefined) => void
}

function TriStateCheckbox({
  className,
  value,
  onValueChange,
  ...props
}: TriStateCheckboxProps) {
  const handleChange = React.useCallback(() => {
    if (value === true) {
      onValueChange(false)
    } else if (value === false) {
      onValueChange(undefined)
    } else {
      onValueChange(true)
    }
  }, [value, onValueChange])

  return (
    <CheckboxPrimitive.Root
      data-slot="checkbox"
      className={cn(
        "peer border-input dark:bg-input/30 data-[state=checked]:bg-primary data-[state=checked]:text-primary-foreground dark:data-[state=checked]:bg-primary data-[state=checked]:border-primary focus-visible:border-ring focus-visible:ring-ring/50 aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive size-4 shrink-0 rounded-[4px] border shadow-xs transition-shadow outline-none focus-visible:ring-[3px] disabled:cursor-not-allowed disabled:opacity-50",
        className
      )}
      checked={value === undefined ? "indeterminate" : value}
      onCheckedChange={handleChange}
      {...props}
    >
      <CheckboxPrimitive.Indicator
        data-slot="checkbox-indicator"
        className="flex items-center justify-center text-current transition-none"
      >
        {value === undefined ? <MinusIcon className="size-3.5" /> : <CheckIcon className="size-3.5" />}
      </CheckboxPrimitive.Indicator>
    </CheckboxPrimitive.Root>
  )
}

export { TriStateCheckbox }
