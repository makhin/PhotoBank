import { createSlice, type PayloadAction } from '@reduxjs/toolkit';

interface BotState {
  lastError: string | null;
}

const initialState: BotState = {
  lastError: null,
};

export const botSlice = createSlice({
  name: 'bot',
  initialState,
  reducers: {
    setLastError(state, action: PayloadAction<string | null>) {
      state.lastError = action.payload;
    },
  },
});

export const { setLastError } = botSlice.actions;
export default botSlice.reducer;
