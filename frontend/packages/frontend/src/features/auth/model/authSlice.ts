import { createAsyncThunk, createSlice } from '@reduxjs/toolkit';
import type { LoginRequestDto } from '@photobank/shared/types';
import { login } from '@photobank/shared/api';
import { invalidCredentialsMsg } from '@photobank/shared/constants';

interface AuthState {
  loading: boolean;
  error?: string;
}

const initialState: AuthState = {
  loading: false,
  error: undefined,
};

export const loginUser = createAsyncThunk(
  'auth/login',
  async (data: LoginRequestDto, { rejectWithValue }) => {
    try {
      await login(data);
    } catch (e) {
      return rejectWithValue(invalidCredentialsMsg);
    }
  },
);

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    resetError(state) {
      state.error = undefined;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(loginUser.pending, (state) => {
        state.loading = true;
        state.error = undefined;
      })
      .addCase(loginUser.fulfilled, (state) => {
        state.loading = false;
      })
      .addCase(loginUser.rejected, (state, action) => {
        state.loading = false;
        state.error = (action.payload as string) ?? invalidCredentialsMsg;
      });
  },
});

export const { resetError } = authSlice.actions;
export default authSlice.reducer;
