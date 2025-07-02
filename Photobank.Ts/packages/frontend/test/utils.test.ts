import { describe, it, expect } from 'vitest';
import { cn } from '../src/lib/utils';

describe('cn', () => {
  it('joins class names', () => {
    expect(cn('foo', 'bar')).toBe('foo bar');
  });

  it('merges duplicate classes', () => {
    expect(cn('p-2', 'p-4')).toBe('p-4');
  });

  it('ignores falsy values', () => {
    expect(cn('a', undefined as unknown as string, '', false as any, 'b')).toBe('a b');
  });
});
