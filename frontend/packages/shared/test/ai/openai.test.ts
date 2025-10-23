import { beforeEach, describe, expect, it, vi } from 'vitest';

const createMock = vi.fn();

vi.mock('openai', () => {
  class MockAzureOpenAI {
    chat = {
      completions: {
        create: createMock,
      },
    };
  }
  return {
    AzureOpenAI: MockAzureOpenAI,
  };
});

import {
  configureAzureOpenAI,
  createChatCompletion,
  parseQueryWithOpenAI,
} from '../../src/ai/openai';
import { logger } from '../../src/utils/logger';

const baseConfig = {
  endpoint: 'https://example.openai.azure.com',
  apiKey: 'test-key',
  deployment: 'gpt-test',
  apiVersion: '2024-05-01',
};

describe('Azure OpenAI helpers', () => {
  beforeEach(() => {
    createMock.mockReset();
    configureAzureOpenAI(baseConfig);
  });

  it('creates text completions with default deployment', async () => {
    createMock.mockResolvedValue({
      choices: [{ message: { content: 'Hello from Azure' } }],
    });

    const result = await createChatCompletion('Hello?');

    expect(result).toBe('Hello from Azure');
    expect(createMock).toHaveBeenCalledWith(
      expect.objectContaining({
        messages: [{ role: 'user', content: 'Hello?' }],
        model: baseConfig.deployment,
        response_format: { type: 'text' },
      }),
    );
  });

  it('parses structured response into a photo filter', async () => {
    const payload = {
      personNames: ['Alice'],
      tagNames: ['beach'],
      dateFrom: '2024-01-01',
      dateTo: '2024-01-15',
    };

    createMock.mockResolvedValue({
      choices: [{ message: { content: JSON.stringify(payload) } }],
    });

    const filter = await parseQueryWithOpenAI('Find beach photos');

    expect(createMock).toHaveBeenCalledWith(
      expect.objectContaining({
        response_format: expect.objectContaining({ type: 'json_schema' }),
        model: baseConfig.deployment,
      }),
    );
    expect(filter.personNames).toEqual(['Alice']);
    expect(filter.tagNames).toEqual(['beach']);
    expect(filter.dateFrom?.toISOString()).toBe('2024-01-01T00:00:00.000Z');
    expect(filter.dateTo?.toISOString()).toBe('2024-01-15T00:00:00.000Z');
  });

  it('logs and normalises errors from the OpenAI client', async () => {
    const error = new Error('Network error');
    createMock.mockRejectedValue(error);

    const errorSpy = vi.spyOn(logger, 'error').mockImplementation(() => {});

    await expect(createChatCompletion('Hi')).rejects.toThrow('OpenAI request failed');
    expect(errorSpy).toHaveBeenCalledWith(error);

    errorSpy.mockRestore();
  });
});

