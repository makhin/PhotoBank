import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import * as z from 'zod';
import { format } from 'date-fns';
import { Save, X, Plus, Trash2 } from 'lucide-react';

import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/shared/ui/dialog';
import { Button } from '@/shared/ui/button';
import { Input } from '@/shared/ui/input';
import { Textarea } from '@/shared/ui/textarea';
import { Label } from '@/shared/ui/label';
import { Checkbox } from '@/shared/ui/checkbox';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/shared/ui/form';
import { Badge } from '@/shared/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card';
import { Separator } from '@/shared/ui/separator';
import { useToast } from '@/hooks/use-toast';

import { mockStorages, mockPersonGroups } from '@/data/mockData';

const formSchema = z.object({
  name: z.string().min(1, 'Profile name is required').max(128, 'Name must be 128 characters or less'),
  description: z.string().min(1, 'Description is required').max(512, 'Description must be 512 characters or less'),
  flags_CanSeeNsfw: z.boolean(),
  storages: z.array(z.number()).min(1, 'At least one storage is required'),
  personGroups: z.array(z.number()).min(1, 'At least one person group is required'),
  dateRanges: z.array(z.object({
    fromDate: z.string(),
    toDate: z.string(),
  })).min(1, 'At least one date range is required'),
});

interface CreateProfileDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CreateProfileDialog({ open, onOpenChange }: CreateProfileDialogProps) {
  const { toast } = useToast();
  const [isLoading, setIsLoading] = useState(false);
  const [dateRanges, setDateRanges] = useState<{ fromDate: string; toDate: string; }[]>([]);

  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      name: '',
      description: '',
      flags_CanSeeNsfw: false,
      storages: [],
      personGroups: [],
      dateRanges: [],
    },
  });

  const onSubmit = async (values: z.infer<typeof formSchema>) => {
    setIsLoading(true);
    try {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      toast({
        title: 'Profile Created',
        description: `${values.name} has been successfully created.`,
      });
      
      form.reset();
      setDateRanges([]);
      onOpenChange(false);
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to create profile. Please try again.',
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const addDateRange = () => {
    const today = new Date();
    const oneYearLater = new Date();
    oneYearLater.setFullYear(today.getFullYear() + 1);
    
    const newRange = {
      fromDate: format(today, 'yyyy-MM-dd'),
      toDate: format(oneYearLater, 'yyyy-MM-dd'),
    };
    
    setDateRanges([...dateRanges, newRange]);
  };

  const removeDateRange = (index: number) => {
    setDateRanges(dateRanges.filter((_, i) => i !== index));
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[700px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="text-xl font-semibold">Create Access Profile</DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
            {/* General Information */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">General Information</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <FormField
                  control={form.control}
                  name="name"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Profile Name *</FormLabel>
                      <FormControl>
                        <Input placeholder="e.g., Editors – Summer" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="description"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Description *</FormLabel>
                      <FormControl>
                        <Textarea 
                          placeholder="Describe the purpose and scope of this access profile..."
                          {...field} 
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="flags_CanSeeNsfw"
                  render={({ field }) => (
                    <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3 shadow-sm">
                      <div className="space-y-0.5">
                        <FormLabel>NSFW Content Access</FormLabel>
                        <div className="text-[0.8rem] text-muted-foreground">
                          Allow access to adult or sensitive content
                        </div>
                      </div>
                      <FormControl>
                        <Checkbox
                          checked={field.value}
                          onCheckedChange={field.onChange}
                        />
                      </FormControl>
                    </FormItem>
                  )}
                />
              </CardContent>
            </Card>

            {/* Access Rules */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Access Rules</CardTitle>
              </CardHeader>
              <CardContent className="space-y-6">
                {/* Storages */}
                <FormField
                  control={form.control}
                  name="storages"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Storages *</FormLabel>
                      <div className="grid grid-cols-2 gap-2">
                        {mockStorages.map((storage) => (
                          <div key={storage.id} className="flex items-center space-x-2">
                            <Checkbox
                              id={`storage-${storage.id}`}
                              checked={field.value.includes(storage.id)}
                              onCheckedChange={(checked) => {
                                if (checked) {
                                  field.onChange([...field.value, storage.id]);
                                } else {
                                  field.onChange(field.value.filter((s) => s !== storage.id));
                                }
                              }}
                            />
                            <Label htmlFor={`storage-${storage.id}`} className="text-sm">
                              {storage.name}
                            </Label>
                          </div>
                        ))}
                      </div>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <Separator />

                {/* Person Groups */}
                <FormField
                  control={form.control}
                  name="personGroups"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Person Groups *</FormLabel>
                      <div className="grid grid-cols-2 gap-2">
                        {mockPersonGroups.map((group) => (
                          <div key={group.id} className="flex items-center space-x-2">
                             <Checkbox
                               id={`group-${group.id}`}
                               checked={field.value.includes(group.id)}
                               onCheckedChange={(checked) => {
                                 if (checked) {
                                   field.onChange([...field.value, group.id]);
                                 } else {
                                   field.onChange(field.value.filter((g) => g !== group.id));
                                 }
                               }}
                             />
                             <Label htmlFor={`group-${group.id}`} className="text-sm">
                               {group.name}
                             </Label>
                          </div>
                        ))}
                      </div>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <Separator />

                {/* Date Ranges */}
                <div>
                  <div className="flex items-center justify-between mb-3">
                    <Label>Date Ranges *</Label>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={addDateRange}
                    >
                      <Plus className="w-4 h-4 mr-2" />
                      Add Range
                    </Button>
                  </div>
                  
                  {dateRanges.length === 0 ? (
                    <p className="text-sm text-muted-foreground text-center py-4 border border-dashed rounded-lg">
                      No date ranges defined. Click "Add Range" to add one.
                    </p>
                  ) : (
                    <div className="space-y-2">
                      {dateRanges.map((range, index) => (
                        <div key={index} className="flex items-center gap-2 p-3 bg-muted/50 rounded-lg">
                          <span className="text-sm font-medium">From:</span>
                          <Badge variant="outline">{range.fromDate}</Badge>
                          <span className="text-sm font-medium">To:</span>
                          <Badge variant="outline">{range.toDate}</Badge>
                          <Button
                            type="button"
                            variant="ghost"
                            size="sm"
                            onClick={() => removeDateRange(index)}
                            className="ml-auto text-destructive hover:text-destructive"
                          >
                            <Trash2 className="w-4 h-4" />
                          </Button>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>


            <div className="flex justify-end gap-2 pt-4">
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
                disabled={isLoading}
              >
                <X className="w-4 h-4 mr-2" />
                Cancel
              </Button>
              <Button type="submit" disabled={isLoading}>
                <Save className="w-4 h-4 mr-2" />
                {isLoading ? 'Creating...' : 'Create Profile'}
              </Button>
            </div>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
}