import type { ViewerItem } from './state';
import { prefetch } from '@/hooks/useImagePrefetch';

export const prefetchAround = (
  items: ViewerItem[],
  index: number,
  radius = 1
) => {
  for (let i = Math.max(0, index - radius); i <= Math.min(items.length - 1, index + radius); i++) {
    if (i === index) continue;
    prefetch(items[i].preview);
    prefetch(items[i].original);
  }
};
