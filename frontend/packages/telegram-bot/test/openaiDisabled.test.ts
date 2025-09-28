import { describe, it, expect, vi } from 'vitest';

describe('bot without OpenAI', () => {
  it('starts without configuring OpenAI when env variables are missing', async () => {
    delete process.env.VITE_AZURE_OPENAI_ENDPOINT;
    delete process.env.VITE_AZURE_OPENAI_KEY;
    delete process.env.VITE_AZURE_OPENAI_DEPLOYMENT;
    delete process.env.VITE_AZURE_OPENAI_API_VERSION;

    vi.resetModules();

    vi.mock('@photobank/shared/ai/openai', () => ({
      configureAzureOpenAI: vi.fn(),
    }));
    vi.mock('../src/dictionaries', () => ({ loadDictionaries: vi.fn(), setDictionariesUser: vi.fn() }));
    vi.mock('../src/commands/thisday', () => ({ sendThisDayPage: vi.fn(), thisDayCommand: vi.fn() }));
    vi.mock('../src/photo', () => ({ captionCache: new Map<number, string>() }));
    vi.mock('../src/commands/search', () => ({ sendSearchPage: vi.fn(), searchCommand: vi.fn(), decodeSearchCallback: vi.fn(() => null) }));
    vi.mock('../src/commands/ai', () => ({ aiCommand: vi.fn(), sendAiPage: vi.fn() }));
    vi.mock('../src/commands/help', () => ({ helpCommand: vi.fn() }));
    vi.mock('../src/commands/subscribe', () => ({
      subscribeCommand: vi.fn(),
      initSubscriptionScheduler: vi.fn(),
      restoreSubscriptions: vi.fn(() => Promise.resolve()),
    }));
    vi.mock('../src/commands/tags', () => ({ registerTagsDictionary: vi.fn() }));
    vi.mock('../src/commands/persons', () => ({ registerPersonsDictionary: vi.fn() }));
    vi.mock('../src/commands/storages', () => ({ registerStoragesDictionary: vi.fn() }));
    vi.mock('../src/commands/photoRouter', () => ({ registerPhotoRoutes: vi.fn() }));
    vi.mock('../src/commands/profile', () => ({ profileCommand: vi.fn() }));
    vi.mock('../src/commands/upload', () => ({ uploadCommand: vi.fn() }));
    vi.mock('../src/registration', () => ({ withRegistered: (fn: any) => fn }));
    vi.mock('../src/logger', () => ({
      logger: { info: vi.fn(), warn: vi.fn(), error: vi.fn() },
    }));
    vi.mock('../src/errorHandler', () => ({ handleBotError: vi.fn() }));
    vi.mock('../src/handlers/inline', () => ({}));
    vi.mock('../src/handlers/deeplink', () => ({}));
    vi.mock('../src/i18n', () => ({
      i18n: {
        t: vi.fn(() => ''),
        useLocale: vi.fn(),
        middleware: vi.fn(() => (_ctx: unknown, next: () => Promise<void>) => next()),
        getLocale: vi.fn(() => Promise.resolve('en')),
      },
    }));
    vi.mock('../src/bot', () => ({
      bot: {
        use: vi.fn(),
        command: vi.fn(),
        callbackQuery: vi.fn(),
        on: vi.fn(),
        api: { setMyCommands: vi.fn() },
        catch: vi.fn(),
        start: vi.fn(),
      },
    }));

    await import('../src/index');

    const openai = await import('@photobank/shared/ai/openai');
    const { bot } = await import('../src/bot');
    const { logger } = await import('../src/logger');

    expect(openai.configureAzureOpenAI).not.toHaveBeenCalled();
    expect(bot.start).toHaveBeenCalled();
    expect(logger.warn).toHaveBeenCalledWith(expect.stringContaining('OpenAI'));
  });
});
