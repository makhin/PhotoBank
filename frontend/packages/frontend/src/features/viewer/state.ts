import { create } from 'zustand';

export interface ViewerItem { id: number; src: string; thumb?: string; title?: string; }

interface ViewerState {
  isOpen: boolean;
  items: ViewerItem[];
  index: number;
  open: (items: ViewerItem[], index: number) => void;
  close: () => void;
  next: () => void;
  prev: () => void;
  setIndex: (i: number) => void;
}

export const useViewer = create<ViewerState>((set, get) => ({
  isOpen: false,
  items: [],
  index: 0,
  open: (items, index) => set({ isOpen: true, items, index }),
  close: () => set({ isOpen: false }),
  next: () => {
    const { index, items } = get();
    if (items.length === 0) return;
    set({ index: (index + 1) % items.length });
  },
  prev: () => {
    const { index, items } = get();
    if (items.length === 0) return;
    set({ index: (index - 1 + items.length) % items.length });
  },
  setIndex: (i) => set({ index: i }),
}));
