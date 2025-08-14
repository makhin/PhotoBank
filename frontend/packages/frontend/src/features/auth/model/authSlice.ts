import { createAsyncThunk, createSlice } from '@reduxjs/toolkit';
import type { LoginRequestDto } from '@photobank/shared/api/photobank';
import { setAuthToken } from '@photobank/shared/auth';
import { invalidCredentialsMsg } from '@photobank/shared/constants';

import { photobankApi } from '@/shared/api.ts';

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
  async (data: LoginRequestDto, { dispatch, rejectWithValue }) => {
    try {
      const res = await dispatch(photobankApi.endpoints.login.initiate(data)).unwrap();
      setAuthToken(res.token, data.rememberMe ?? true);
    } catch {
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
