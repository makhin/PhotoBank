import { describe, it, expect, vi, beforeEach } from 'vitest';

const point = { latitude: 10, longitude: 20 };

describe('getPlaceByGeoPoint', () => {
  beforeEach(() => {
    vi.resetModules();
  });

  it('requests Nominatim and returns address', async () => {
    const getMock = vi.fn().mockResolvedValue({ data: { display_name: 'Nice place' } });
    vi.doMock('axios', () => ({ default: { get: getMock } }));
    const { getPlaceByGeoPoint } = await import('../src/utils/geocode');
    const result = await getPlaceByGeoPoint(point);
    expect(getMock).toHaveBeenCalledWith('https://nominatim.openstreetmap.org/reverse', {
      params: { format: 'json', lat: 10, lon: 20 },
      headers: { 'User-Agent': 'photobank' },
    });
    expect(result).toBe('Nice place');
  });

  it('falls back to coordinates when request fails', async () => {
    const getMock = vi.fn().mockRejectedValue(new Error('fail'));
    vi.doMock('axios', () => ({ default: { get: getMock } }));
    const { getPlaceByGeoPoint } = await import('../src/utils/geocode');
    const result = await getPlaceByGeoPoint(point);
    expect(result).toBe('10.0000, 20.0000');
  });
});
