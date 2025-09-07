import { createSelector } from '@reduxjs/toolkit';
import type { RootState } from '@/app/store';

export const selectMetadataLoaded = (state: RootState) => state.metadata.loaded;

export const selectPersonsMap = createSelector(
  (state: RootState) => state.metadata.persons,
  (persons) => new Map(persons.map((p) => [p.id, p.name])),
);

export const selectTagsMap = createSelector(
  (state: RootState) => state.metadata.tags,
  (tags) => new Map(tags.map((t) => [t.id, t.name])),
);
