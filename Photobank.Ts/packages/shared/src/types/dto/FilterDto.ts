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
  /**
   * Hash of the filter that can be used for caching.
   * This property is not sent to the backend and is
   * computed on the client side only.
   */
  hash?: string;
}
