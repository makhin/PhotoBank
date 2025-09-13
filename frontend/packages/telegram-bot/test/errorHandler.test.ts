import { describe, it, expect, vi } from 'vitest';
import { ProblemDetailsError } from '@photobank/shared/types/problem';
import { apiErrorMsg } from '@photobank/shared/constants';

import { handleCommandError } from '../src/errorHandler';
import { i18n } from '../src/i18n';
import { logger } from '../src/logger';

describe('handleCommandError', () => {
  it('handles forbidden error', async () => {
    const ctx: any = {
      reply: vi.fn(),
      from: { id: 42 },
      t: (k: string, params?: any) => i18n.t('en', k, params),
    };
    const error = new ProblemDetailsError({ title: 'forbidden', status: 403 });

    await handleCommandError(ctx, error);

    expect(ctx.reply).toHaveBeenCalledWith(
      i18n.t('en', 'not-registered', { userId: 42 }),
    );
  });

  it('logs generic errors', async () => {
    const ctx: any = {
      reply: vi.fn(),
      from: { id: 42 },
      t: (k: string, params?: any) => i18n.t('en', k, params),
    };
    const error = new Error('oops');
    const spy = vi.spyOn(logger, 'error').mockImplementation(() => {});

    await handleCommandError(ctx, error);

    expect(spy).toHaveBeenCalledWith(apiErrorMsg, error);
    expect(ctx.reply).toHaveBeenCalledWith(i18n.t('en', 'sorry-try-later'));

    spy.mockRestore();
  });
});

