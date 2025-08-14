import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';


const renderPage = async (regMock: any) => {
  vi.doMock('../src/shared/api.ts', () => ({
    useRegisterMutation: () => [regMock, { isLoading: false }],
  }));
  const { default: RegisterPage } = await import('../src/pages/auth/RegisterPage');
  render(
    <MemoryRouter initialEntries={["/register"]}>
      <Routes>
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/login" element={<div>Login</div>} />
      </Routes>
    </MemoryRouter>
  );
};

describe('RegisterPage', () => {
  beforeEach(() => {
    vi.resetModules();
    vi.clearAllMocks();
  });

  it('submits registration data', async () => {
    const regMock = vi
      .fn()
      .mockReturnValue({ unwrap: () => Promise.resolve({}) });
    await renderPage(regMock);
    await userEvent.type(screen.getByLabelText('Email'), 'a@b.co');
    await userEvent.type(screen.getByLabelText('Password'), '123');
    await userEvent.click(screen.getByRole('button', { name: /register/i }));
    await waitFor(() => {
      expect(regMock).toHaveBeenCalledWith({ email: 'a@b.co', password: '123' });
    });
  });
});
