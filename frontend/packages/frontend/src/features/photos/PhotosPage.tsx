import { useEffect, useState } from 'react';

interface PhotoItem {
  id: string;
}

export function PhotosPage() {
  const [photos, setPhotos] = useState<PhotoItem[]>([]);

  useEffect(() => {
    const load = async () => {
      const profileRes = await fetch('/api/access/profile');
      const profile = await profileRes.json();
      const storageId = profile.storages?.[0];
      const photosRes = await fetch(`/api/photos?storageId=${storageId}`);
      const data = await photosRes.json();
      setPhotos(data.items ?? []);
    };
    void load();
  }, []);

  return (
    <div>
      {photos.map((p) => (
        <div key={p.id}>{p.id}</div>
      ))}
    </div>
  );
}

export default PhotosPage;
