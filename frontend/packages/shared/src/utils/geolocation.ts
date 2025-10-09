import type { GeoPointDto } from '../api/photobank';

export type GeoPointLike =
  | Pick<GeoPointDto, 'latitude' | 'longitude'>
  | {
      latitude?: number | null | undefined;
      longitude?: number | null | undefined;
    }
  | null
  | undefined;

const LATITUDE_RANGE = [-90, 90] as const;
const LONGITUDE_RANGE = [-180, 180] as const;

export function isValidGeoPoint(point: GeoPointLike): point is GeoPointDto {
  if (!point) {
    return false;
  }

  const { latitude, longitude } = point;

  if (latitude == null || longitude == null) {
    return false;
  }

  if (!Number.isFinite(latitude) || !Number.isFinite(longitude)) {
    return false;
  }

  if (latitude < LATITUDE_RANGE[0] || latitude > LATITUDE_RANGE[1]) {
    return false;
  }

  if (longitude < LONGITUDE_RANGE[0] || longitude > LONGITUDE_RANGE[1]) {
    return false;
  }

  return Math.abs(latitude) + Math.abs(longitude) !== 0;
}

export function formatGeoLink(point: GeoPointDto, placeName?: string | null) {
  const href = `https://www.google.com/maps?q=${point.latitude},${point.longitude}`;
  const coordinatesLabel = `${point.latitude.toFixed(5)}, ${point.longitude.toFixed(5)}`;
  const label = placeName?.trim() ? placeName.trim() : coordinatesLabel;

  return {
    href,
    label,
    coordinatesLabel,
  } as const;
}
