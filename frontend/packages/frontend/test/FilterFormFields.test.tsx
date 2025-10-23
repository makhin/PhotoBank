import React from 'react';
import { render, screen } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { useForm } from 'react-hook-form';
import metaReducer from '../src/features/meta/model/metaSlice';
import { Form } from '../src/shared/ui/form';
import { describe, it, beforeEach, expect, vi } from 'vitest';

const renderWithAdmin = async (isAdmin: boolean) => {
  vi.doMock('@photobank/shared', async () => {
    const actual = await vi.importActual<any>('@photobank/shared');
    return {
      ...actual,
      useIsAdmin: () => isAdmin,
      useCanSeeNsfw: () => false,
    };
  });

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
};

describe('FilterFormFields', () => {
  beforeEach(() => {
    vi.resetModules();
    vi.clearAllMocks();
  });

  it('shows admin checkboxes for administrators', async () => {
    await renderWithAdmin(true);
    expect(await screen.findByText('Adult Content')).toBeTruthy();
  });

});
