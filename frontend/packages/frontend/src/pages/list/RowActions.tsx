import { memo, useCallback } from 'react';
import { MoreHorizontal } from 'lucide-react';
import { Button } from '@/shared/ui/button';

interface RowActionsProps {
  id: number;
  onOpen?: (id: number) => void;
}

const RowActions = ({ id, onOpen }: RowActionsProps) => {
  const handleClick = useCallback(() => onOpen?.(id), [onOpen, id]);
  return (
    <Button
      aria-label="More actions"
      variant="ghost"
      size="icon"
      onClick={handleClick}
    >
      <MoreHorizontal className="w-4 h-4" />
    </Button>
  );
};

export default memo(RowActions);
