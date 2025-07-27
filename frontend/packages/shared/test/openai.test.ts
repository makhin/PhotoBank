import { describe, it, expect, beforeEach, vi } from 'vitest';

const cfg = {
  endpoint: 'https://example.openai.azure.com',
  apiKey: 'key',
  deployment: 'gpt-4',
  apiVersion: '2024-02-15-preview',
};

describe('azure openai service', () => {
  beforeEach(() => {
    vi.resetModules();
  });

  it('posts chat completion request', async () => {
    const postMock = vi.fn().mockResolvedValue({ data: { id: '1', choices: [] } });
    vi.doMock('axios', () => ({ default: { post: postMock } }));
    const { configureAzureOpenAI, createChatCompletion } = await import('../src/api/openai');
    configureAzureOpenAI(cfg);
    const req = { messages: [{ role: 'user', content: 'hi' }] };
    const res = await createChatCompletion(req);
    expect(postMock).toHaveBeenCalledWith(
      `${cfg.endpoint}/openai/deployments/${cfg.deployment}/chat/completions?api-version=${cfg.apiVersion}`,
      req,
      { headers: { 'api-key': cfg.apiKey } },
    );
    expect(res).toEqual({ id: '1', choices: [] });
  });

  it('throws when not configured', async () => {
    const { createChatCompletion } = await import('../src/api/openai');
    await expect(createChatCompletion({ messages: [] })).rejects.toThrow('Azure OpenAI is not configured');
  });
});
