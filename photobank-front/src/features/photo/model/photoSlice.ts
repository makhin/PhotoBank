import { createSlice, type PayloadAction } from '@reduxjs/toolkit';

import type { FilterDto, PhotoItemDto } from '@/entities/photo/model.ts';

interface PhotoState {
  filter: FilterDto;
  selectedPhotos: number[];
  lastResult: PhotoItemDto[];
}

const initialState: PhotoState = {
  filter: {},
  selectedPhotos: [],
  lastResult: [],
};

const photoSlice = createSlice({
  name: 'photo',
  initialState,
  reducers: {
    setFilter(state, action: PayloadAction<FilterDto>) {
      state.filter = action.payload;
    },
    resetFilter(state) {
      state.filter = {};
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
    setLastResult(state, action: PayloadAction<PhotoItemDto[]>) {
      state.lastResult = action.payload;
    },
  },
});

export const {
  setFilter,
  resetFilter,
  setSelectedPhotos,
  addSelectedPhoto,
  removeSelectedPhoto,
  setLastResult,
} = photoSlice.actions;

export default photoSlice.reducer;
