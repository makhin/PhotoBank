import type { ViewerItem } from './viewerSlice';

const prefetchImage = (src: string) => {
  if (!src) return;
  const img = new Image();
  img.onload = () => undefined;
  img.onerror = () => undefined;
  img.src = src;
};

export const prefetchAround = (items: ViewerItem[], index: number, radius = 1) => {
  for (let i = Math.max(0, index - radius); i <= Math.min(items.length - 1, index + radius); i++) {
    if (i === index) continue;
    const item = items[i];
    if (!item?.preview) continue;
    prefetchImage(item.preview);
  }
};

export default prefetchAround;
