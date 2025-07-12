import { describe, it, expect } from 'vitest';
import reducer, {
  addSelectedPhoto,
  removeSelectedPhoto,
  setFilter,
  resetFilter,
  setLastResult,
} from '../src/features/photo/model/photoSlice';
import { DEFAULT_PHOTO_FILTER } from '@photobank/shared/constants';

describe('photoSlice', () => {
  it('sets and resets filter', () => {
    let state = reducer(undefined, setFilter({ caption: 'foo' } as any));
    expect(state.filter).toEqual({ caption: 'foo' });
    state = reducer(state, resetFilter());
    expect(state.filter).toEqual(DEFAULT_PHOTO_FILTER);
  });

  it('manages selected photos without duplicates', () => {
    let state = reducer(undefined, addSelectedPhoto(1));
    state = reducer(state, addSelectedPhoto(1));
    expect(state.selectedPhotos).toEqual([1]);
    state = reducer(state, addSelectedPhoto(2));
    expect(state.selectedPhotos).toEqual([1, 2]);
    state = reducer(state, removeSelectedPhoto(1));
    expect(state.selectedPhotos).toEqual([2]);
  });

  it('updates last result', () => {
    const photos = [{ id: 5 } as any];
    const state = reducer(undefined, setLastResult(photos));
    expect(state.lastResult).toBe(photos);
  });
});

