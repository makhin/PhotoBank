import type { ViewerItem } from './state';

export const prefetchAround = (
  items: ViewerItem[],
  index: number,
  radius = 1
) => {
  for (let i = Math.max(0, index - radius); i <= Math.min(items.length - 1, index + radius); i++) {
    if (i === index) continue;
    const img = new Image();
    img.src = items[i].src;
  }
};
