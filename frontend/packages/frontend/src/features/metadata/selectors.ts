import { createSelector } from '@reduxjs/toolkit';
import {
  buildPersonMap,
  buildTagMap,
  type PersonMap,
  type TagMap,
} from '@photobank/shared/metadata';

import type { RootState } from '@/app/store';

export const selectMetadataLoaded = (state: RootState) => state.metadata.loaded;

export const selectPersonsMap = createSelector(
  (state: RootState) => state.metadata.persons,
  (persons): PersonMap => buildPersonMap(persons),
);

export const selectTagsMap = createSelector(
  (state: RootState) => state.metadata.tags,
  (tags): TagMap => buildTagMap(tags),
);
