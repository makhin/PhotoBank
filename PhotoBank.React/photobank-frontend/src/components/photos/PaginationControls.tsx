import { Button } from '@/components/ui/button';

interface Props {
  skip: number;
  top: number;
  totalCount: number;
  setSkip: (value: number) => void;
}

export default function PaginationControls({ skip, top, totalCount, setSkip }: Props) {
  return (
      <div className="flex flex-col items-center gap-2">
        <div>Показано {skip + 1}–{Math.min(skip + top, totalCount)} из {totalCount}</div>
        <div className="flex gap-4">
          <Button onClick={() => setSkip(Math.max(0, skip - top))} disabled={skip === 0}>Назад</Button>
          <Button onClick={() => setSkip(skip + top)} disabled={skip + top >= totalCount}>Вперёд</Button>
        </div>
      </div>
  );
}
