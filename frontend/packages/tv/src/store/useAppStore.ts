// src/store/useAppStore.ts
import { create } from 'zustand';
import { devtools, persist, createJSONStorage } from 'zustand/middleware';
import AsyncStorage from '@react-native-async-storage/async-storage';
import type { PersonDto, TagDto } from '@/api';

interface User {
  id: string;
  name: string;
  email: string;
}

export interface CredentialsState {
  username: string;
  password: string;
}

interface AppState {
  // State
  user: User | null;
  theme: 'light' | 'dark';
  isLoading: boolean;
  focusedItemId: string | null;
  persons: PersonDto[];
  tags: TagDto[];
  credentials: CredentialsState;

  // Actions
  setUser: (user: User | null) => void;
  setTheme: (theme: 'light' | 'dark') => void;
  setIsLoading: (isLoading: boolean) => void;
  setFocusedItemId: (id: string | null) => void;
  setPersons: (persons: PersonDto[]) => void;
  setTags: (tags: TagDto[]) => void;
  setCredentials: (credentials: Partial<CredentialsState>) => void;
  reset: () => void;
}

const initialState = {
  user: null,
  theme: 'dark' as const,
  isLoading: false,
  focusedItemId: null,
  persons: [],
  tags: [],
  credentials: {
    username: '',
    password: '',
  },
};

export const useAppStore = create<AppState>()(
  devtools(
    persist(
      (set) => ({
        ...initialState,

        setUser: (user) => set({ user }, false, 'setUser'),

        setTheme: (theme) => set({ theme }, false, 'setTheme'),

        setIsLoading: (isLoading) => set({ isLoading }, false, 'setIsLoading'),

        setFocusedItemId: (focusedItemId) => set({ focusedItemId }, false, 'setFocusedItemId'),

        setPersons: (persons) => set({ persons }, false, 'setPersons'),

        setTags: (tags) => set({ tags }, false, 'setTags'),

        setCredentials: (credentials) =>
          set(
            (state) => ({
              credentials: {
                ...state.credentials,
                ...credentials,
              },
            }),
            false,
            'setCredentials'
          ),

        reset: () => set(initialState, false, 'reset'),
      }),
      {
        name: 'app-storage',
        storage: createJSONStorage(() => AsyncStorage),
      }
    ),
    { name: 'AppStore' }
  )
);

export const selectUser = (state: AppState) => state.user;
export const selectTheme = (state: AppState) => state.theme;
export const selectIsLoading = (state: AppState) => state.isLoading;
export const selectFocusedItemId = (state: AppState) => state.focusedItemId;
export const selectPersons = (state: AppState) => state.persons;
export const selectTags = (state: AppState) => state.tags;
export const selectCredentials = (state: AppState) => state.credentials;
export const selectUsername = (state: AppState) => state.credentials.username;
export const selectPassword = (state: AppState) => state.credentials.password;
