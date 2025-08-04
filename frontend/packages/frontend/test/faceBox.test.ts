import { describe, it, expect } from 'vitest';
import type { FaceBoxDto } from '@photobank/shared/generated';
import { transformFaceBox } from '../src/lib/faceBox';

describe('transformFaceBox', () => {
  const box: FaceBoxDto = { left: 10, top: 20, width: 30, height: 40 };

  it('returns same box for orientation 1', () => {
    expect(transformFaceBox(box, 1, 100, 200)).toEqual(box);
  });

  it('rotates box for orientation 6', () => {
    const result = transformFaceBox(box, 6, 100, 200);
    expect(result).toEqual({ left: 20, top: 100 - 10 - 30, width: 40, height: 30 });
  });

  it('rotates box for orientation 8', () => {
    const result = transformFaceBox(box, 8, 100, 200);
    expect(result).toEqual({ left: 200 - 20 - 40, top: 10, width: 40, height: 30 });
  });
});

