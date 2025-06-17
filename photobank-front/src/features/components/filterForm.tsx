"use client";

import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import {Cat, Dog, Turtle, Rabbit, Fish} from "lucide-react";
import {MultiSelect} from "@/components/ui/multi-select.tsx";
import {DatePickerWithRange} from "@/components/ui/date-picker-range.tsx";
import {useState} from "react";

type DateRange = {
  from: Date | null;
  to: Date | null;
};

interface FormData {
  select1: string;
  select2: string;
  select3: string;
  select4: string;
  checkbox1: boolean;
  checkbox2: boolean;
  checkbox3: boolean;
  checkbox4: boolean;
  inputText: string;
  dateRange: DateRange;
}

export function FilterForm() {
  const form = useForm<FormData>({
    defaultValues: {
      select1: "",
      select2: "",
      select3: "",
      select4: "",
      checkbox1: false,
      checkbox2: false,
      checkbox3: false,
      checkbox4: false,
      inputText: "",
      dateRange: { from: null, to: null },
    },
  });

  function onSubmit(data: FormData) {
    console.log("Submitted:", data);
  }

    const [selectedFrameworks, setSelectedFrameworks] = useState<string[]>(["react", "angular"]);

    const frameworksList = [
        { value: "react", label: "React", icon: Turtle },
        { value: "angular", label: "Angular", icon: Cat },
        { value: "vue", label: "Vue", icon: Dog },
        { value: "svelte", label: "Svelte", icon: Rabbit },
        { value: "ember", label: "Ember", icon: Fish },
    ];

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="w-full max-w-screen-xl mx-auto grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {/* Select fields */}
        <FormField
          control={form.control}
          name="select1"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Select 1</FormLabel>
              <FormControl>
                  <MultiSelect {...field}
                               options={frameworksList}
                               onValueChange={setSelectedFrameworks}
                               defaultValue={selectedFrameworks}
                               placeholder="Select frameworks"
                               variant="inverted"
                  />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="select2"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Select 2</FormLabel>
              <FormControl>
                  <MultiSelect {...field}
                               options={frameworksList}
                               onValueChange={setSelectedFrameworks}
                               defaultValue={selectedFrameworks}
                               placeholder="Select frameworks"
                               variant="inverted"
                  />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="select3"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Select 3</FormLabel>
              <FormControl>
                  <MultiSelect {...field}
                               options={frameworksList}
                               onValueChange={setSelectedFrameworks}
                               defaultValue={selectedFrameworks}
                               placeholder="Select frameworks"
                               variant="inverted"
                  />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="select4"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Select 4</FormLabel>
              <FormControl>
                  <MultiSelect {...field}
                               options={frameworksList}
                               onValueChange={setSelectedFrameworks}
                               defaultValue={selectedFrameworks}
                               placeholder="Select frameworks"
                               variant="inverted"
                  />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Checkbox fields */}
        <FormField
          control={form.control}
          name="checkbox1"
          render={({ field }) => (
            <FormItem className="flex flex-row items-center gap-3">
              <FormControl>
                <Checkbox 
                  checked={field.value} 
                  onCheckedChange={field.onChange} 
                  id="checkbox-1" 
                />
              </FormControl>
              <FormLabel htmlFor="checkbox-1">Checkbox 1</FormLabel>
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="checkbox2"
          render={({ field }) => (
            <FormItem className="flex flex-row items-center gap-3">
              <FormControl>
                <Checkbox 
                  checked={field.value} 
                  onCheckedChange={field.onChange} 
                  id="checkbox-2" 
                />
              </FormControl>
              <FormLabel htmlFor="checkbox-2">Checkbox 2</FormLabel>
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="checkbox3"
          render={({ field }) => (
            <FormItem className="flex flex-row items-center gap-3">
              <FormControl>
                <Checkbox 
                  checked={field.value} 
                  onCheckedChange={field.onChange} 
                  id="checkbox-3" 
                />
              </FormControl>
              <FormLabel htmlFor="checkbox-3">Checkbox 3</FormLabel>
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="checkbox4"
          render={({ field }) => (
            <FormItem className="flex flex-row items-center gap-3">
              <FormControl>
                <Checkbox 
                  checked={field.value} 
                  onCheckedChange={field.onChange} 
                  id="checkbox-4" 
                />
              </FormControl>
              <FormLabel htmlFor="checkbox-4">Checkbox 4</FormLabel>
            </FormItem>
          )}
        />

        {/* Text input field */}
        <FormField
          control={form.control}
          name="inputText"
          render={({ field }) => (
            <FormItem className="col-span-4">
              <FormLabel>Text Input</FormLabel>
              <FormControl>
                <Input
                  className="w-full" 
                  {...field} 
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Date range picker field */}
        <FormField
          control={form.control}
          name="dateRange"
          render={({ field }) => (
            <FormItem className="col-span-4">
              <FormLabel>Date Range</FormLabel>
              <FormControl>
                  <DatePickerWithRange {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Action buttons */}
        <div className="col-span-4 flex justify-end gap-4">
          <Button type="button" variant="outline" onClick={() => form.reset()}>
            Reset
          </Button>
          <Button type="submit">
            Submit
          </Button>
        </div>
      </form>
    </Form>
  );
}
