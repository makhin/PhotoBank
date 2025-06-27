import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';
import {format, parseISO} from "date-fns";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export const formatDate = (dateString?: string) => {
  if (!dateString) return 'Not specified';
  try {
    return format(parseISO(dateString), 'dd.MM.yyyy, HH:mm');
  } catch {
    return 'Invalid date';
  }
};

export const getGenderText = (gender?: boolean) => {
  if (gender === undefined) return 'Unknown';
  return gender ? 'Female' : 'Male';
};


