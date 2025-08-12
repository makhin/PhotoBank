import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { useForm } from 'react-hook-form';
import metaReducer from '../src/features/meta/model/metaSlice';
import { Form } from '../src/components/ui/form';
import { describe, it, beforeEach, expect, vi } from 'vitest';

class RO {
  observe() {}
  unobserve() {}
  disconnect() {}
}
// @ts-ignore
global.ResizeObserver = RO;

declare module '@testing-library/react' {
  interface RenderOptions {
    wrapper?: React.ComponentType;
  }
}

const renderWithRoles = async (roles: any[]) => {
  const getUserRoles = vi.fn().mockImplementation(() => {
    console.log('getUserRoles called with', roles);
    return Promise.resolve({ data: roles });
  });
  vi.doMock('@photobank/shared/auth', () => ({
    getAuthToken: () => 'token',
  }));
  vi.doMock('@photobank/shared/api/photobank', () => ({
    authGetUserRoles: getUserRoles,
  }));

  const { FilterFormFields } = await import('../src/components/FilterFormFields');

  const store = configureStore({
    reducer: { metadata: metaReducer },
    preloadedState: {
      metadata: {
        tags: [],
        persons: [],
        paths: [],
        storages: [],
        version: 1,
        loaded: true,
        loading: false,
        error: undefined,
      },
    },
  });

  function Wrapper() {
    const form = useForm({
      defaultValues: {
        caption: '',
        storages: [],
        paths: [],
        persons: [],
        tags: [],
        isBW: undefined,
        isAdultContent: undefined,
        isRacyContent: undefined,
        thisDay: undefined,
        dateFrom: undefined,
        dateTo: undefined,
      },
    });

    return (
      <Provider store={store}>
        <Form {...form}>
          <form>
            <FilterFormFields control={form.control} />
          </form>
        </Form>
      </Provider>
    );
  }

  render(<Wrapper />);
  return { getUserRoles };
};

describe('FilterFormFields', () => {
  beforeEach(() => {
    vi.resetModules();
    vi.clearAllMocks();
  });

  it.skip('shows admin checkboxes for administrators', async () => {
    await renderWithRoles([{ name: 'Administrator' }]);
    expect(await screen.findByText('Adult Content')).toBeTruthy();
  });

});
