import axios from 'axios';
import type { GeoPointDto } from '../types';
import { GOOGLE_API_KEY } from '../config';

/**
 * Returns a human friendly place name for the given coordinates using
 * Google Geocoding API. Falls back to "lat, lng" when the request fails
 * or API key is not provided.
 */
export async function getPlaceByGeoPoint(point: GeoPointDto): Promise<string> {
  if (!GOOGLE_API_KEY) {
    return `${point.latitude.toFixed(4)}, ${point.longitude.toFixed(4)}`;
  }

  try {
    const res = await axios.get('https://maps.googleapis.com/maps/api/geocode/json', {
      params: {
        latlng: `${point.latitude},${point.longitude}`,
        key: GOOGLE_API_KEY,
      },
    });
    const name = res.data?.results?.[0]?.formatted_address;
    return name ?? `${point.latitude.toFixed(4)}, ${point.longitude.toFixed(4)}`;
  } catch {
    return `${point.latitude.toFixed(4)}, ${point.longitude.toFixed(4)}`;
  }
}
