import { describe, it, expect, vi, afterEach } from 'vitest';
import * as shared from '@photobank/shared';
import { formatPhotoMessage } from '../src/formatPhotoMessage';
import type { PhotoDto } from '@photobank/shared/api/photobank';

vi.mock('../src/dictionaries', () => ({
  getPersonName: (id: number | null | undefined) => id == null ? 'Неизвестный' : `Person ${id}`,
}));

afterEach(() => {
  vi.restoreAllMocks();
});

describe('formatPhotoMessage', () => {
  const basePhoto: PhotoDto = {
    id: 1,
    name: 'Test',
    scale: 1,
    adultScore: 0,
    racyScore: 0,
    height: 100,
    width: 100,
  };

  it('includes main fields in caption', async () => {
    const { caption, imageUrl } = await formatPhotoMessage({
      ...basePhoto,
      takenDate: '2024-01-02T00:00:00Z',
      captions: ['hello'],
      tags: ['tag1', 'tag2'],
      faces: [{ id: 1, personId: 2, faceBox: { top:0, left:0, width:1, height:1 }, friendlyFaceAttributes: '' }],
    });
    expect(caption).toContain('📸 <b>Test</b>');
    expect(caption).toContain('📅');
    expect(caption).toContain('📝 hello');
    expect(caption).toContain('🏷️ tag1, tag2');
    expect(caption).toContain('👤 Person 2');
    expect(imageUrl).toBeUndefined();
  });

  it('uses preview url when provided', async () => {
    const { imageUrl } = await formatPhotoMessage({
      ...basePhoto,
      previewUrl: 'http://example.com/preview.jpg',
    });
    expect(imageUrl).toBe('http://example.com/preview.jpg');
  });

  it('replaces missing person with unknown label', async () => {
    const { caption } = await formatPhotoMessage({
      ...basePhoto,
      faces: [{ id: 1, personId: null, faceBox: { top:0, left:0, width:1, height:1}, friendlyFaceAttributes: '' }],
    });
    expect(caption).toContain('👤 Неизвестный');
  });

  it('adds geo point link when location is present', async () => {
    const getPlaceByGeoPointSpy = vi
      .spyOn(shared, 'getPlaceByGeoPoint')
      .mockResolvedValue('Mock Address');

    const { caption } = await formatPhotoMessage({
      ...basePhoto,
      location: { latitude: 10, longitude: 20 },
    });
    expect(caption).toContain('📍');
    expect(caption).toContain('https://www.google.com/maps?q=10,20');
    expect(caption).toContain('Mock Address');
    expect(getPlaceByGeoPointSpy).toHaveBeenCalledWith({ latitude: 10, longitude: 20 });
  });

  it('omits location when coordinates are zero', async () => {
    const getPlaceByGeoPointSpy = vi
      .spyOn(shared, 'getPlaceByGeoPoint')
      .mockResolvedValue('Should not be used');

    const { caption } = await formatPhotoMessage({
      ...basePhoto,
      location: { latitude: 0, longitude: 0 },
    });

    expect(getPlaceByGeoPointSpy).not.toHaveBeenCalled();
    expect(caption).not.toContain('📍');
  });
});
