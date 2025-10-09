import { describe, expect, it } from 'vitest';

import { formatGeoLink, isValidGeoPoint } from '../src/utils/geolocation';

describe('isValidGeoPoint', () => {
  it('returns false for missing point', () => {
    expect(isValidGeoPoint(null)).toBe(false);
    expect(isValidGeoPoint(undefined)).toBe(false);
  });

  it('rejects incomplete coordinates', () => {
    expect(isValidGeoPoint({ latitude: 10, longitude: null })).toBe(false);
    expect(isValidGeoPoint({ latitude: null, longitude: 10 })).toBe(false);
  });

  it('rejects coordinates outside allowed ranges', () => {
    expect(isValidGeoPoint({ latitude: 95, longitude: 10 })).toBe(false);
    expect(isValidGeoPoint({ latitude: 45, longitude: -190 })).toBe(false);
  });

  it('rejects origin point', () => {
    expect(isValidGeoPoint({ latitude: 0, longitude: 0 })).toBe(false);
  });

  it('accepts valid coordinates', () => {
    expect(isValidGeoPoint({ latitude: 10, longitude: 20 })).toBe(true);
  });
});

describe('formatGeoLink', () => {
  it('uses place name when provided', () => {
    const result = formatGeoLink({ latitude: 10.123456, longitude: 20.654321 }, ' Test Place ');

    expect(result.href).toBe('https://www.google.com/maps?q=10.123456,20.654321');
    expect(result.label).toBe('Test Place');
    expect(result.coordinatesLabel).toBe('10.12346, 20.65432');
  });

  it('falls back to formatted coordinates when place name is missing', () => {
    const result = formatGeoLink({ latitude: -1, longitude: 2 }, '');

    expect(result.label).toBe('-1.00000, 2.00000');
  });
});
