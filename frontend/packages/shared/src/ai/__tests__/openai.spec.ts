import { beforeEach, describe, expect, it, vi } from 'vitest';
import { logger } from '../../utils/logger';

const mockCreate = vi.fn();

vi.mock('openai', () => ({
  AzureOpenAI: vi.fn().mockImplementation(() => ({
    chat: { completions: { create: mockCreate } },
  })),
}));

import { configureAzureOpenAI, createChatCompletion, parseQueryWithOpenAI } from '../openai';

describe('openai', () => {
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

  describe('error handling', () => {
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

  describe('model selection', () => {
    it('createChatCompletion uses the configured deployment as the model', async () => {
      mockCreate.mockResolvedValueOnce({
        choices: [{ message: { content: 'hello' } }],
      });

      await expect(createChatCompletion('hi')).resolves.toBe('hello');

      expect(mockCreate).toHaveBeenCalledWith(
        expect.objectContaining({ model: 'deploy' }),
      );
    });

    it('parseQueryWithOpenAI uses the configured deployment as the model', async () => {
      mockCreate.mockResolvedValueOnce({
        choices: [
          {
            message: {
              content: JSON.stringify({
                personNames: [],
                tagNames: [],
                dateFrom: null,
                dateTo: null,
              }),
            },
          },
        ],
      });

      await parseQueryWithOpenAI('query');

      expect(mockCreate).toHaveBeenCalledWith(
        expect.objectContaining({ model: 'deploy' }),
      );
    });
  });

  describe('configuration validation', () => {
    it('throws if the deployment is missing', async () => {
      configureAzureOpenAI({
        endpoint: 'test',
        apiKey: 'key',
        deployment: '',
        apiVersion: 'v1',
      });

      await expect(createChatCompletion('hi')).rejects.toThrow(
        'Azure OpenAI deployment is not configured',
      );
      expect(mockCreate).not.toHaveBeenCalled();
    });
  });
});
