import { describe, expect, it } from 'vitest';

import { parseArgsToFilter } from './search';

describe('parseArgsToFilter before:... handling', () => {
  it('excludes entire year when using before:YYYY', () => {
    const filter = parseArgsToFilter('before:2015');

    expect(filter.takenDateTo?.toISOString()).toBe('2014-12-31T23:59:59.999Z');
  });

  it('excludes entire month when using before:YYYY-MM', () => {
    const filter = parseArgsToFilter('before:2015-06');

    expect(filter.takenDateTo?.toISOString()).toBe('2015-05-31T23:59:59.999Z');
  });

  it('excludes the given day when using before:YYYY-MM-DD', () => {
    const filter = parseArgsToFilter('before:2015-06-10');

    expect(filter.takenDateTo?.toISOString()).toBe('2015-06-09T23:59:59.999Z');
  });
});
