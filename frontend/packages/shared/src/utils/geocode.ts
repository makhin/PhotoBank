import axios from 'axios';
import type { GeoPointDto } from '../generated';

/**
 * Returns a human friendly place name for the given coordinates using
 * the Nominatim reverse geocoding API. Falls back to "lat, lng" when the
 * request fails.
 */
export async function getPlaceByGeoPoint(point: GeoPointDto): Promise<string> {
  try {
    const res = await axios.get('https://nominatim.openstreetmap.org/reverse', {
      params: {
        format: 'json',
        lat: point.latitude,
        lon: point.longitude,
      },
      headers: {
        'User-Agent': 'photobank',
      },
    });

    const name = res.data?.display_name as string | undefined;
    return name ?? `${point.latitude.toFixed(4)}, ${point.longitude.toFixed(4)}`;
  } catch {
    return `${point.latitude.toFixed(4)}, ${point.longitude.toFixed(4)}`;
  }
}
