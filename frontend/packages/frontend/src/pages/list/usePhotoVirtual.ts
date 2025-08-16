import { useVirtualizer } from '@tanstack/react-virtual';
import { RefObject } from 'react';

interface UsePhotoVirtualProps {
  count: number;
  parentRef: RefObject<HTMLElement>;
  estimateSize?: (index: number) => number;
}

export const usePhotoVirtual = ({
  count,
  parentRef,
  estimateSize,
}: UsePhotoVirtualProps) => {
  const virtualizer = useVirtualizer({
    count,
    getScrollElement: () => {
      const parent = parentRef.current;
      if (!parent) return null;
      const viewport = parent.querySelector<HTMLElement>(
        '[data-slot="scroll-area-viewport"]'
      );
      return viewport ?? (parent);
    },
    estimateSize: estimateSize ?? (() => 112),
    overscan: 8,
  });

  const items = virtualizer.getVirtualItems();
  const totalSize = virtualizer.getTotalSize();

  return { virtualizer, items, totalSize };
};

export default usePhotoVirtual;
