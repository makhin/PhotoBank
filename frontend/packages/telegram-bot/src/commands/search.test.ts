import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { FilterDto } from '@photobank/shared/api/photobank';

vi.mock('./photosPage', () => ({
  sendPhotosPage: vi.fn(),
}));

import { sendSearchPage, decodeSearchCallback } from './search';
import { sendPhotosPage } from './photosPage';
import { clearSearchFilterTokens } from '../cache/searchFilterCache';
import type { MyContext } from '../i18n';

const sendPhotosPageMock = vi.mocked(sendPhotosPage);

function createContext(): MyContext {
  return {
    t: vi.fn((key: string) => key),
  } as unknown as MyContext;
}

describe('sendSearchPage', () => {
  beforeEach(() => {
    clearSearchFilterTokens();
    sendPhotosPageMock.mockReset();
    sendPhotosPageMock.mockResolvedValue(undefined);
  });

  it('creates compact callback tokens that round-trip long filters', async () => {
    const ctx = createContext();
    const filter: FilterDto = {
      caption: 'a'.repeat(256),
      tagNames: Array.from({ length: 8 }, (_, i) => `tag-${'x'.repeat(16)}-${i}`),
      personNames: Array.from({ length: 5 }, (_, i) => `person-${'y'.repeat(12)}-${i}`),
    };

    await sendSearchPage(ctx, filter, 42);

    expect(sendPhotosPageMock).toHaveBeenCalledTimes(1);
    const callArgs = sendPhotosPageMock.mock.calls[0];
    const options = callArgs ? callArgs[0] : undefined;
    expect(options?.buildCallbackData).toBeTypeOf('function');

    const callbackData = options!.buildCallbackData(42);
    expect(callbackData.length).toBeLessThanOrEqual(64);

    const decoded = decodeSearchCallback(callbackData);
    expect(decoded).not.toBeNull();
    expect(decoded?.page).toBe(42);
    expect(decoded?.filter).toEqual(filter);
  });
});