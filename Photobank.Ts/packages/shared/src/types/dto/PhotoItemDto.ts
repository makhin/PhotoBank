import type { TagItemDto } from './TagItemDto';
import type { PersonItemDto } from './PersonItemDto';

export interface PhotoItemDto {
  id: number;
  thumbnail: string;
  name: string;
  takenDate?: string;
  isBW: boolean;
  isAdultContent: boolean;
  adultScore: number;
  isRacyContent: boolean;
  racyScore: number;
  storageName: string;
  relativePath: string;
  tags?: TagItemDto[];
  persons?: PersonItemDto[];
}