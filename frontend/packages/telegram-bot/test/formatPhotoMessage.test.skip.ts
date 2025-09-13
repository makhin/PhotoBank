import { describe, it, expect, vi } from 'vitest';
import { formatPhotoMessage } from '../src/formatPhotoMessage';
import type { PhotoDto } from '@photobank/shared/api/photobank';

vi.mock('../src/dictionaries', () => ({
  getPersonName: (id: number | null | undefined) => id == null ? 'Неизвестный' : `Person ${id}`,
}));

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

  it('includes main fields in caption', () => {
    const { caption, imageUrl } = formatPhotoMessage({
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

  it('uses preview url when provided', () => {
    const { imageUrl } = formatPhotoMessage({ ...basePhoto, previewUrl: 'http://example.com/preview.jpg' });
    expect(imageUrl).toBe('http://example.com/preview.jpg');
  });

  it('replaces missing person with unknown label', () => {
    const { caption } = formatPhotoMessage({
      ...basePhoto,
      faces: [{ id: 1, personId: null, faceBox: { top:0, left:0, width:1, height:1}, friendlyFaceAttributes: '' }],
    });
    expect(caption).toContain('👤 Неизвестный');
  });

  it('adds geo point link when location is present', () => {
    const { caption } = formatPhotoMessage({
      ...basePhoto,
      location: { latitude: 10, longitude: 20 },
    });
    expect(caption).toContain('📍');
    expect(caption).toContain('https://www.google.com/maps?q=10,20');
    expect(caption).toContain('10.00000, 20.00000');
  });
});
