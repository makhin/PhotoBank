"use client";

import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";
import { Popover, PopoverTrigger, PopoverContent } from "@/components/ui/popover";
import { Calendar } from "@/components/ui/calendar";
import { Select, SelectTrigger, SelectContent, SelectItem, SelectValue } from "@/components/ui/select";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { CalendarIcon } from "lucide-react";

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

export function DemoForm() {
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
                <Select onValueChange={field.onChange} defaultValue={field.value}>
                  <SelectTrigger className="w-full">
                    <SelectValue placeholder="???????? ???????" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="option1">Option 1</SelectItem>
                    <SelectItem value="option2">Option 2</SelectItem>
                    <SelectItem value="option3">Option 3</SelectItem>
                  </SelectContent>
                </Select>
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
                <Select onValueChange={field.onChange} defaultValue={field.value}>
                  <SelectTrigger className="w-full">
                    <SelectValue placeholder="???????? ???????" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="option1">Option 1</SelectItem>
                    <SelectItem value="option2">Option 2</SelectItem>
                    <SelectItem value="option3">Option 3</SelectItem>
                  </SelectContent>
                </Select>
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
                <Select onValueChange={field.onChange} defaultValue={field.value}>
                  <SelectTrigger className="w-full">
                    <SelectValue placeholder="???????? ???????" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="option1">Option 1</SelectItem>
                    <SelectItem value="option2">Option 2</SelectItem>
                    <SelectItem value="option3">Option 3</SelectItem>
                  </SelectContent>
                </Select>
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
                <Select onValueChange={field.onChange} defaultValue={field.value}>
                  <SelectTrigger className="w-full">
                    <SelectValue placeholder="???????? ???????" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="option1">Option 1</SelectItem>
                    <SelectItem value="option2">Option 2</SelectItem>
                    <SelectItem value="option3">Option 3</SelectItem>
                  </SelectContent>
                </Select>
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
                  placeholder="??????? ?????..." 
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
                <Popover>
                  <PopoverTrigger asChild>
                    <Button variant="outline" className="w-full justify-between">
                      <CalendarIcon className="mr-2 h-4 w-4" />
                      {field.value?.from && field.value.to ? (
                        <>
                          {field.value.from.toLocaleDateString()} ? {field.value.to.toLocaleDateString()}
                        </>
                      ) : field.value?.from ? (
                        field.value.from.toLocaleDateString()
                      ) : (
                        "???????? ????"
                      )}
                    </Button>
                  </PopoverTrigger>
                  <PopoverContent className="w-auto p-0" align="start">
                    <Calendar 
                      mode="range" 
                      selected={field.value ?? null}
                      onSelect={field.onChange} 
                      numberOfMonths={2} 
                      initialFocus 
                    />
                  </PopoverContent>
                </Popover>
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
