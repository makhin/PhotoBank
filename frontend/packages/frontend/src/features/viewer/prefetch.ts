import { prefetch } from '@/hooks/useImagePrefetch';

import type { ViewerItem } from './state';

export const prefetchAround = (
  items: ViewerItem[],
  index: number,
  radius = 1
) => {
  for (let i = Math.max(0, index - radius); i <= Math.min(items.length - 1, index + radius); i++) {
    if (i === index) continue;
    const item = items[i];
    if (!item) continue;
    prefetch(item.preview);
    prefetch(item.original);
  }
};
