import { prefetchAround } from '@/features/viewer/prefetch';

describe('prefetchAround', () => {
  it('loads adjacent images', () => {
    const items = [
      { id: 1, preview: 'a_p' },
      { id: 2, preview: 'b_p' },
      { id: 3, preview: 'c_p' },
    ];
    const loaded: string[] = [];
    // @ts-ignore
    global.Image = class {
      set src(v: string) {
        loaded.push(v);
      }
    };
    prefetchAround(items, 1);
    expect(loaded).toContain('a_p');
    expect(loaded).toContain('c_p');
    expect(loaded).not.toContain('b_p');
  });
});
