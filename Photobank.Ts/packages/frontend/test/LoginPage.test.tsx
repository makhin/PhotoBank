import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

class RO {
  observe() {}
  unobserve() {}
  disconnect() {}
}
// @ts-ignore
global.ResizeObserver = RO;

const renderPage = async (loginMock: any) => {
  vi.doMock('@photobank/shared/api', () => ({ login: loginMock }));
  const { default: LoginPage } = await import('../src/pages/auth/LoginPage');
  render(
    <MemoryRouter initialEntries={["/login"]}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
      </Routes>
    </MemoryRouter>
  );
};

describe('LoginPage', () => {
  beforeEach(() => {
    vi.resetModules();
    vi.clearAllMocks();
  });

  it('shows error when login fails', async () => {
    const loginMock = vi.fn().mockRejectedValue(new Error('bad'));
    await renderPage(loginMock);

    fireEvent.input(screen.getByLabelText('Email'), { target: { value: 'e@e.com' } });
    fireEvent.input(screen.getByLabelText('Password'), { target: { value: 'p' } });
    fireEvent.click(screen.getByRole('button', { name: /login/i }));

    await screen.findByRole('alert');
    expect(loginMock).toHaveBeenCalled();
  });
});
