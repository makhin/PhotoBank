import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { isBrowser } from '../src/utils/isBrowser';

type GlobalWithDom = typeof globalThis & {
  window?: unknown;
  document?: unknown;
};
const globalWithDom = globalThis as GlobalWithDom;

describe('isBrowser', () => {
  beforeEach(() => {
    delete globalWithDom.window;
    delete globalWithDom.document;
  });

  afterEach(() => {
    delete globalWithDom.window;
    delete globalWithDom.document;
  });

  it('returns false when window or document are undefined', () => {
    expect(isBrowser()).toBe(false);
  });

  it('returns true when window and document are defined', () => {
    globalWithDom.window = {};
    globalWithDom.document = {};
    expect(isBrowser()).toBe(true);
  });
});

