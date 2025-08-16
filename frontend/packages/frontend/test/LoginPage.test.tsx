import React from 'react';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const renderPage = async (loginMock: any) => {
  vi.doMock('@photobank/shared/api/photobank/auth/auth', () => ({
    useAuthLogin: (opts: any) => ({
      mutate: (args: any) =>
        loginMock(args).then(
          (res: any) => opts?.mutation?.onSuccess?.(res, args, undefined),
          (err: any) => opts?.mutation?.onError?.(err, args, undefined),
        ),
      isPending: false,
    }),
  }));
  vi.doMock('@photobank/shared/auth', () => ({ setAuthToken: vi.fn() }));
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

    await userEvent.type(screen.getByLabelText('Email'), 'e@e.com');
    await userEvent.type(screen.getByLabelText('Password'), 'p');
    await userEvent.click(screen.getByRole('button', { name: /login/i }));

    await screen.findByRole('alert');
    expect(loginMock).toHaveBeenCalled();
  });

  it('toggles password visibility', async () => {
    const loginMock = vi.fn();
    await renderPage(loginMock);

    const passwordField = screen.getAllByTestId('password-input')[0];
    const toggle = screen.getAllByRole('button', { name: /show password/i })[0];

    expect(passwordField.getAttribute('type')).toBe('password');

    await userEvent.click(toggle);
    expect(passwordField.getAttribute('type')).toBe('text');
    expect(toggle.textContent).toBe('Hide password');

    await userEvent.click(toggle);
    expect(passwordField.getAttribute('type')).toBe('password');
  });
});
