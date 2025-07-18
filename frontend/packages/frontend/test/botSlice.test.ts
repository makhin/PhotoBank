import { describe, it, expect } from 'vitest';
import reducer, { setLastError } from '../src/features/bot/model/botSlice';

describe('botSlice', () => {
  it('sets lastError correctly', () => {
    let state = reducer(undefined, setLastError('oops'));
    expect(state.lastError).toBe('oops');
    state = reducer(state, setLastError(null));
    expect(state.lastError).toBeNull();
  });
});
