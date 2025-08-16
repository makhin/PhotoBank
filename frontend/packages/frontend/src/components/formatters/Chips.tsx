import { memo, useCallback, useMemo } from 'react';
import { formatList } from '@photobank/shared/format';

import { Badge } from '@/shared/ui/badge';

interface ChipsProps {
  items: string[];
  max?: number;
  onClickItem?: (value: string) => void;
}

const ChipsComponent = ({ items, max, onClickItem }: ChipsProps) => {
  const handleClick = useCallback(
    (value: string) => {
      onClickItem?.(value);
    },
    [onClickItem]
  );

  const list = useMemo(() => formatList(items, max), [items, max]);

  return (
    <div className="flex flex-wrap gap-1">
      {list.visible.map((item) => (
        <Badge
          key={item}
          variant="secondary"
          className="text-xs"
          onClick={onClickItem ? () => handleClick(item) : undefined}
        >
          {item}
        </Badge>
      ))}
      {list.hidden > 0 && (
        <Badge variant="outline" className="text-xs">+{list.hidden}</Badge>
      )}
    </div>
  );
};

export default memo(ChipsComponent);
