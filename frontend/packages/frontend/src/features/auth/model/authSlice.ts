import { createSlice } from '@reduxjs/toolkit';

interface AuthState {
  loading: boolean;
  error?: string;
}

const initialState: AuthState = {
  loading: false,
  error: undefined,
};

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    resetError(state) {
      state.error = undefined;
    },
  },
});

export const { resetError } = authSlice.actions;
export default authSlice.reducer;
