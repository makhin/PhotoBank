import { describe, it, expect, vi } from 'vitest';
import { formatPhotoMessage } from '../src/utils/formatPhotoMessage';
import type { PhotoDto } from '../src/generated';

vi.mock('../src/dictionaries', () => ({
  getPersonName: (id: number | null | undefined) => id == null ? 'Неизвестный' : `Person ${id}`,
}));

describe('formatPhotoMessage', () => {
  const basePhoto: PhotoDto = {
    id: 1,
    name: 'Test',
    scale: 1,
    previewImage: '',
    adultScore: 0,
    racyScore: 0,
    height: 100,
    width: 100,
  };

  it('includes main fields in caption', () => {
    const { caption, image } = formatPhotoMessage({
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
    expect(image).toBeUndefined();
  });

  it('decodes preview image when provided', () => {
    const base64 = Buffer.from('img').toString('base64');
    const { image } = formatPhotoMessage({ ...basePhoto, previewImage: base64 });
    expect(image).toBeInstanceOf(Buffer);
    expect(image?.toString()).toBe('img');
  });

  it('replaces missing person with unknown label', () => {
    const { caption } = formatPhotoMessage({
      ...basePhoto,
      faces: [{ id: 1, personId: null, faceBox: { top:0, left:0, width:1, height:1}, friendlyFaceAttributes: '' }],
    });
    expect(caption).toContain('👤 Неизвестный');
  });
});
