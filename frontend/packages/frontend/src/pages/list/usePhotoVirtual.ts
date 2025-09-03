import { useVirtualizer } from '@tanstack/react-virtual';
import { RefObject } from 'react';

interface UsePhotoVirtualProps {
  count: number;
  viewportRef: RefObject<HTMLElement | null>;
  estimateSize?: (index: number) => number;
}

export const usePhotoVirtual = ({
  count,
  viewportRef,
  estimateSize,
}: UsePhotoVirtualProps) => {
  const virtualizer = useVirtualizer({
    count,
    getScrollElement: () => viewportRef.current,
    estimateSize: estimateSize ?? (() => 112),
    overscan: 8,
  });

  const items = virtualizer.getVirtualItems();
  const totalSize = virtualizer.getTotalSize();

  return { virtualizer, items, totalSize };
};

export default usePhotoVirtual;
