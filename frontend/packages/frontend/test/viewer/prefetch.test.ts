import { prefetchAround } from '@/features/viewer/prefetch';

describe('prefetchAround', () => {
  it('loads adjacent images', () => {
    const items = [
      { id: 1, src: 'a' },
      { id: 2, src: 'b' },
      { id: 3, src: 'c' },
    ];
    const loaded: string[] = [];
    // @ts-ignore
    global.Image = class {
      set src(v: string) {
        loaded.push(v);
      }
    };
    prefetchAround(items, 1);
    expect(loaded).toContain('a');
    expect(loaded).toContain('c');
    expect(loaded).not.toContain('b');
  });
});
