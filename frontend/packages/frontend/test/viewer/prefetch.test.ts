import { prefetchAround } from '@/features/viewer/prefetch';

describe('prefetchAround', () => {
  it('loads adjacent images', () => {
    const items = [
      { id: 1, preview: 'a_p', original: 'a_o' },
      { id: 2, preview: 'b_p', original: 'b_o' },
      { id: 3, preview: 'c_p', original: 'c_o' },
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
    expect(loaded).toContain('a_o');
    expect(loaded).toContain('c_p');
    expect(loaded).toContain('c_o');
    expect(loaded).not.toContain('b_p');
    expect(loaded).not.toContain('b_o');
  });
});
