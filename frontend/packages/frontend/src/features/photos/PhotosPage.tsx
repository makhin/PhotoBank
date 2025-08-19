import { useEffect, useState } from 'react';
import type { PhotoItemDto } from '@photobank/shared/api/photobank';

interface AccessProfile {
  storages?: string[];
}

export const PhotosPage = () => {
  const [photos, setPhotos] = useState<PhotoItemDto[]>([]);

  useEffect(() => {
    let active = true;
    const load = async () => {
      const profileRes = await fetch('/api/access/profile');
      const profile: AccessProfile = await profileRes.json();
      const allowedStorages = profile.storages ?? [];

      const photosRes = await fetch('/api/photos');
      const data = await photosRes.json();
      const filtered = (data.items ?? []).filter((p: PhotoItemDto) =>
        allowedStorages.length === 0 || allowedStorages.includes(p.storageId ?? '')
      );
      if (active) {
        setPhotos(filtered);
      }
    };
    load();
    return () => {
      active = false;
    };
  }, []);

  return (
    <div>
      {photos.map((p) => (
        <div key={p.id}>{p.id}</div>
      ))}
    </div>
  );
};

export default PhotosPage;
