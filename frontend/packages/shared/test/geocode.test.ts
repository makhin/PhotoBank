import { describe, it, expect, vi, beforeEach } from 'vitest';

const point = { latitude: 10, longitude: 20 };

describe('getPlaceByGeoPoint', () => {
  beforeEach(() => {
    vi.resetModules();
  });

  it('requests google with api key and returns address', async () => {
    const getMock = vi.fn().mockResolvedValue({ data: { results: [{ formatted_address: 'Nice place' }] } });
    vi.doMock('axios', () => ({ default: { get: getMock } }));
    vi.doMock('../src/config', () => ({ GOOGLE_API_KEY: 'abc' }));
    const { getPlaceByGeoPoint } = await import('../src/utils/geocode');
    const result = await getPlaceByGeoPoint(point);
    expect(getMock).toHaveBeenCalledWith('https://maps.googleapis.com/maps/api/geocode/json', {
      params: { latlng: '10,20', key: 'abc' },
    });
    expect(result).toBe('Nice place');
  });

  it('falls back to coordinates when request fails', async () => {
    const getMock = vi.fn().mockRejectedValue(new Error('fail'));
    vi.doMock('axios', () => ({ default: { get: getMock } }));
    vi.doMock('../src/config', () => ({ GOOGLE_API_KEY: 'abc' }));
    const { getPlaceByGeoPoint } = await import('../src/utils/geocode');
    const result = await getPlaceByGeoPoint(point);
    expect(result).toBe('10.0000, 20.0000');
  });
});
