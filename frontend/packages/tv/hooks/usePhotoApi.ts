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
    setLoading(true);
       postApiPhotosSearch(filter)
      .then((res) => { setPhotos(res.data.photos || []); })
      .finally(() => { setLoading(false); });
  }, [filter]);

  return { photos, loading };
};

export const usePhotoById = (id: number) => {
  const [photo, setPhoto] = useState<PhotoDto>();
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    setLoading(true);
    getApiPhotos(id)
        .then((res) => { setPhoto(res.data); })
        .finally(() => { setLoading(false); });
  }, [id]);

  return { photo, loading };
};
