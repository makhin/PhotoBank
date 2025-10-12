import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { formatDateTime } from '@photobank/shared/format';

import { I18nProvider } from '@/app/providers/I18nProvider';

import { PhotoPropertiesPanel } from './PhotoPropertiesPanel';
import type { PhotoDetails } from '../types';

describe('PhotoPropertiesPanel', () => {
  it('displays the taken date including hours and minutes', () => {
    const takenDate = new Date(2024, 5, 7, 15, 30);
    const photo: PhotoDetails = {
      id: 1,
      name: 'Sunset over the bay',
      takenDate,
      adultScore: 0,
      racyScore: 0,
    };

    render(
      <I18nProvider>
        <PhotoPropertiesPanel
          photo={photo}
          formattedTakenDate={formatDateTime(takenDate)}
        />
      </I18nProvider>
    );

    expect(
      screen.getByDisplayValue(formatDateTime(takenDate))
    ).toBeInTheDocument();
  });
});
