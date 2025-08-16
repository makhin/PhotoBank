import { createSlice, type PayloadAction } from '@reduxjs/toolkit';
import type { FilterDto } from '@photobank/shared/api/photobank';
import {
  DEFAULT_PHOTO_FILTER,
  PHOTO_FILTER_STORAGE_KEY,
} from '@photobank/shared/constants';
import { namespacedStorage } from '@photobank/shared/safeStorage';

interface PhotoState {
  filter: FilterDto;
  selectedPhotos: number[];
}

const store = namespacedStorage('photo');

const initialState: PhotoState = {
  filter: store.get<FilterDto>(PHOTO_FILTER_STORAGE_KEY) ?? {
    ...DEFAULT_PHOTO_FILTER,
  },
  selectedPhotos: [],
};

const photoSlice = createSlice({
  name: 'photo',
  initialState,
  reducers: {
    setFilter(state, action: PayloadAction<FilterDto>) {
      state.filter = action.payload;
    },
    resetFilter(state) {
      state.filter = { ...DEFAULT_PHOTO_FILTER };
    },
    setSelectedPhotos(state, action: PayloadAction<number[]>) {
      state.selectedPhotos = action.payload;
    },
    addSelectedPhoto(state, action: PayloadAction<number>) {
      if (!state.selectedPhotos.includes(action.payload)) {
        state.selectedPhotos.push(action.payload);
      }
    },
    removeSelectedPhoto(state, action: PayloadAction<number>) {
      state.selectedPhotos = state.selectedPhotos.filter(
        (id) => id !== action.payload
      );
    },
  },
});

export const {
  setFilter,
  resetFilter,
  setSelectedPhotos,
  addSelectedPhoto,
  removeSelectedPhoto,
} = photoSlice.actions;

export default photoSlice.reducer;
