import { useEffect, useState } from "react";
import {PhotoItemDto, FilterDto, PhotoDto} from "@photobank/shared/types";
import {getPhotoById, searchPhotos} from "@photobank/shared/api";

export const usePhotos = (filter: FilterDto | null) => {
  const [photos, setPhotos] = useState<PhotoItemDto[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!filter) return;
    setLoading(true);
       searchPhotos(filter)
      .then((data) => setPhotos(data.photos || []))
      .finally(() => setLoading(false));
  }, [filter]);

  return { photos, loading };
};

export const usePhotoById = (id: number) => {
  const [photo, setPhoto] = useState<PhotoDto>();
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    setLoading(true);
    getPhotoById(id)
        .then((data) => setPhoto(data))
        .finally(() => setLoading(false));
  }, [id]);

  return { photo, loading };
};
