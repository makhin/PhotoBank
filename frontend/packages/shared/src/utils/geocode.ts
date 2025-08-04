import axios from 'axios';

import type { GeoPointDto } from '../generated';

/**
 * Returns a human friendly place name for the given coordinates using
 * the Nominatim reverse geocoding API. Falls back to "lat, lng" when the
 * request fails.
 */
export async function getPlaceByGeoPoint(point: GeoPointDto): Promise<string> {
  const lat = point.latitude ?? 0;
  const lon = point.longitude ?? 0;
  try {
    const res = await axios.get<{ display_name?: string }>(
      'https://nominatim.openstreetmap.org/reverse',
      {
        params: {
          format: 'json',
          lat,
          lon,
        },
        headers: {
          'User-Agent': 'photobank',
        },
      }
    );

    const name = res.data.display_name;
    return name ?? `${lat.toFixed(4)}, ${lon.toFixed(4)}`;
  } catch {
    return `${lat.toFixed(4)}, ${lon.toFixed(4)}`;
  }
}
