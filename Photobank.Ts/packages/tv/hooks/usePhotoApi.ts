import { useEffect, useState } from "react";
import { PhotoItemDto, FilterDto } from "@photobank/shared/types";
import { apiClient } from "@photobank/shared/api";

export const usePhotos = (filter: FilterDto | null) => {
  const [photos, setPhotos] = useState<PhotoItemDto[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!filter) return;
    setLoading(true);
    apiClient
      .searchPhotos(filter)
      .then((data) => setPhotos(data.photos || []))
      .finally(() => setLoading(false));
  }, [filter]);

  return { photos, loading };
};
