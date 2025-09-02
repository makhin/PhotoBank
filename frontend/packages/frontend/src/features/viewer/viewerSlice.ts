import { createSlice, type PayloadAction } from '@reduxjs/toolkit';

export interface ViewerItem {
  id: number;
  preview: string;
  title?: string;
}

interface ViewerState {
  isOpen: boolean;
  items: ViewerItem[];
  index: number;
}

const initialState: ViewerState = {
  isOpen: false,
  items: [],
  index: 0,
};

const viewerSlice = createSlice({
  name: 'viewer',
  initialState,
  reducers: {
    open: (
      state,
      action: PayloadAction<{ items: ViewerItem[]; index: number }>
    ) => {
      state.isOpen = true;
      state.items = action.payload.items;
      state.index = action.payload.index;
    },
    close: (state) => {
      state.isOpen = false;
    },
    next: (state) => {
      if (state.items.length === 0) return;
      state.index = (state.index + 1) % state.items.length;
    },
    prev: (state) => {
      if (state.items.length === 0) return;
      state.index = (state.index - 1 + state.items.length) % state.items.length;
    },
    setIndex: (state, action: PayloadAction<number>) => {
      state.index = action.payload;
    },
  },
});

export const { open, close, next, prev, setIndex } = viewerSlice.actions;
export default viewerSlice.reducer;
export type { ViewerState };
