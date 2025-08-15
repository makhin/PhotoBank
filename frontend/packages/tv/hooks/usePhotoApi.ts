import { useEffect, useState } from "react";
import {
  PhotoItemDto,
  FilterDto,
  PhotoDto,
  postApiPhotosSearch,
  getApiPhotos,
} from '@photobank/shared/api/photobank';

export const usePhotos = (filter: FilterDto | null) => {
  const [photos, setPhotos] = useState<PhotoItemDto[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!filter) return;
    let ignore = false;
    (async () => {
      setLoading(true);
      try {
        const res = await postApiPhotosSearch(filter);
        if (!ignore) setPhotos(res.data.photos || []);
      } finally {
        if (!ignore) setLoading(false);
      }
    })();
    return () => {
      ignore = true;
    };
  }, [filter]);

  return { photos, loading };
};

export const usePhotoById = (id: number) => {
  const [photo, setPhoto] = useState<PhotoDto>();
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    let ignore = false;
    (async () => {
      setLoading(true);
      try {
        const res = await getApiPhotos(id);
        if (!ignore) setPhoto(res.data);
      } finally {
        if (!ignore) setLoading(false);
      }
    })();
    return () => {
      ignore = true;
    };
  }, [id]);

  return { photo, loading };
};
