export interface FilterDto {
  storages?: number[];
  isBW?: boolean;
  isAdultContent?: boolean;
  isRacyContent?: boolean;
  relativePath?: string;
  paths?: number[];
  caption?: string;
  takenDateFrom?: string;
  takenDateTo?: string;
  thisDay?: boolean;
  persons?: number[];
  tags?: number[];
  orderBy?: string;
  skip?: number;
  top?: number;
}