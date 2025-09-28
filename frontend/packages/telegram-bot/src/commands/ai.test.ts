import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { FilterDto } from '@photobank/shared/api/photobank';

vi.mock('./photosPage', () => ({
  sendPhotosPage: vi.fn(),
}));

import { decodeAiCallback, sendAiPage } from './ai';
import { sendPhotosPage } from './photosPage';
import { clearSearchFilterTokens } from '../cache/searchFilterCache';
import type { MyContext } from '../i18n';

const sendPhotosPageMock = vi.mocked(sendPhotosPage);

function createContext(): MyContext {
  return {
    t: vi.fn((key: string) => key),
  } as unknown as MyContext;
}

describe('sendAiPage', () => {
  beforeEach(() => {
    clearSearchFilterTokens();
    sendPhotosPageMock.mockReset();
    sendPhotosPageMock.mockResolvedValue(undefined);
  });

  it('reuses filter tokens for paging callbacks', async () => {
    const ctx = createContext();
    const filter: FilterDto = {
      caption: 'sky photos',
      tagNames: ['clouds', 'sunset'],
      personNames: ['alice'],
    };

    await sendAiPage(ctx, filter, 3);

    expect(sendPhotosPageMock).toHaveBeenCalledTimes(1);
    const callArgs = sendPhotosPageMock.mock.calls[0];
    const options = callArgs ? callArgs[0] : undefined;
    expect(options?.buildCallbackData).toBeTypeOf('function');

    const callbackData = options!.buildCallbackData(5);
    expect(callbackData.startsWith('ai:')).toBe(true);
    expect(callbackData.length).toBeLessThanOrEqual(64);

    const decoded = decodeAiCallback(callbackData);
    expect(decoded).not.toBeNull();
    expect(decoded?.page).toBe(5);
    expect(decoded?.filter).toEqual(filter);
  });
});
