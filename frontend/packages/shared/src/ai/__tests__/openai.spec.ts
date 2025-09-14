import { beforeEach, describe, expect, it, vi } from 'vitest';
import { logger } from '../../utils/logger';

const mockCreate = vi.fn();

vi.mock('openai', () => ({
  AzureOpenAI: vi.fn().mockImplementation(() => ({
    chat: { completions: { create: mockCreate } },
  })),
}));

import { configureAzureOpenAI, createChatCompletion, parseQueryWithOpenAI } from '../openai';

describe('openai error handling', () => {
  const loggerSpy = vi.spyOn(logger, 'error').mockImplementation(() => {});

  beforeEach(() => {
    mockCreate.mockReset();
    loggerSpy.mockClear();
    configureAzureOpenAI({
      endpoint: 'test',
      apiKey: 'key',
      deployment: 'deploy',
      apiVersion: 'v1',
    });
  });

  it('createChatCompletion throws friendly error when request fails', async () => {
    const err = new Error('network');
    mockCreate.mockRejectedValueOnce(err);

    await expect(createChatCompletion('hi')).rejects.toThrow('OpenAI request failed');
    expect(loggerSpy).toHaveBeenCalledWith(err);
  });

  it('parseQueryWithOpenAI throws friendly error when request fails', async () => {
    const err = new Error('oops');
    mockCreate.mockRejectedValueOnce(err);

    await expect(parseQueryWithOpenAI('query')).rejects.toThrow('OpenAI request failed');
    expect(loggerSpy).toHaveBeenCalledWith(err);
  });
});
