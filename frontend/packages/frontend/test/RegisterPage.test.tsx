import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

class RO {
  observe() {}
  unobserve() {}
  disconnect() {}
}
// @ts-ignore
global.ResizeObserver = RO;

const renderPage = async (regMock: any) => {
  vi.doMock('@photobank/shared/api', () => ({ register: regMock }));
  const { default: RegisterPage } = await import('../src/pages/auth/RegisterPage');
  render(
    <MemoryRouter initialEntries={["/register"]}>
      <Routes>
        <Route path="/register" element={<RegisterPage />} />
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
    const regMock = vi.fn().mockResolvedValue({});
    await renderPage(regMock);
    fireEvent.input(screen.getByLabelText('Email'), { target: { value: 'a@b.co' } });
    fireEvent.input(screen.getByLabelText('Password'), { target: { value: '123' } });
    fireEvent.click(screen.getByRole('button', { name: /register/i }));
    await waitFor(() => {
      expect(regMock).toHaveBeenCalledWith({ email: 'a@b.co', password: '123' });
    });
  });
});
