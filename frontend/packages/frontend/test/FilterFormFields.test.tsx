import React from 'react';
import { render, screen } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { useForm } from 'react-hook-form';
import metaReducer from '../src/features/meta/model/metaSlice';
import { Form } from '../src/shared/ui/form';
import { describe, it, beforeEach, afterEach, expect, vi } from 'vitest';

const translationOverrides: Record<string, string> = {
  captionLabel: 'Caption',
  captionPlaceholder: 'Enter caption...',
  dateFromLabel: 'Date From',
  selectDatePlaceholder: 'Select date',
  clearDateButton: 'Clear date',
  dateToLabel: 'Date To',
  storagesLabel: 'Storages',
  selectStoragesPlaceholder: 'Select storages',
  pathsLabel: 'Paths',
  selectPathsPlaceholder: 'Select paths',
  personsLabel: 'Persons',
  selectPersonsPlaceholder: 'Select persons',
  tagsLabel: 'Tags',
  selectTagsPlaceholder: 'Select tags',
  blackWhiteLabel: 'Black-White',
  adultContentLabel: 'Adult Content',
  racyContentLabel: 'Racy Content',
  thisDayLabel: 'This Day',
};

const renderWithAdmin = async (isAdmin: boolean) => {
  vi.doMock('@photobank/shared', async () => {
    const actual = await vi.importActual<typeof import('@photobank/shared')>(
      '@photobank/shared',
    );

    return {
      ...actual,
      useIsAdmin: () => isAdmin,
      useCanSeeNsfw: () => false,
    };
  });

  vi.doMock('react-i18next', () => ({
    useTranslation: () => ({
      t: (key: string) => translationOverrides[key] ?? key,
      i18n: {
        language: 'en',
        changeLanguage: vi.fn(),
        isInitialized: true,
      },
      ready: true,
    }),
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
        personNames: [],
        tagNames: [],
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
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.resetModules();
    vi.unmock('@photobank/shared');
    vi.unmock('react-i18next');
  });

  it('shows admin checkboxes for administrators', async () => {
    await renderWithAdmin(true);
    expect(screen.getByText('Adult Content')).toBeInTheDocument();
  });

});
