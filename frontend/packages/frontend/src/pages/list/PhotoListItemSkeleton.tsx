import { Card } from '@/shared/ui/card';

const PhotoListItemSkeleton = () => (
  <Card className="p-4 mb-3">
    <div className="grid grid-cols-12 gap-4 items-center animate-pulse">
      <div className="col-span-1 h-6 bg-muted rounded" />
      <div className="col-span-2 h-16 bg-muted rounded" />
      <div className="col-span-2 space-y-2">
        <div className="h-4 bg-muted rounded" />
        <div className="h-3 bg-muted rounded w-3/4" />
      </div>
      <div className="col-span-1 h-4 bg-muted rounded" />
      <div className="col-span-2 h-4 bg-muted rounded" />
      <div className="col-span-1 h-4 bg-muted rounded" />
      <div className="col-span-3 space-y-2">
        <div className="h-4 bg-muted rounded" />
        <div className="h-4 bg-muted rounded w-1/2" />
      </div>
    </div>
  </Card>
);

export default PhotoListItemSkeleton;
