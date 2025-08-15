import { memo } from 'react';
import { ImageOff } from 'lucide-react';

interface EmptyStateProps {
  text: string;
}

const EmptyState = ({ text }: EmptyStateProps) => (
  <div className="flex flex-col items-center justify-center py-10 text-muted-foreground">
    <ImageOff className="w-12 h-12 mb-4" aria-hidden="true" />
    <p>{text}</p>
  </div>
);

export default memo(EmptyState);
