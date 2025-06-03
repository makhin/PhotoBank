import { useAppSelector } from '@/app/hooks';

export default function PhotoCards() {
    const photos = useAppSelector(state => state.photos.items) || [];
  return (
      <div className="grid gap-4">
        {photos.map(photo => (
            <div key={photo.id} className="p-2 border rounded">
              {photo.thumbnail && <img src={`data:image/jpeg;base64,${photo.thumbnail}`} alt="preview" className="w-full h-auto" />}
              <div className="text-sm font-medium">{photo.name}</div>
              <div className="text-xs text-muted-foreground">Дата: {photo.takenDate}</div>
            </div>
        ))}
      </div>
  );
}
