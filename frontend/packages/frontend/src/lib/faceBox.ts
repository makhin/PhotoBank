import type { FaceBoxDto } from '@photobank/shared/generated';

/**
 * Transforms face box coordinates according to EXIF orientation.
 * Width and height parameters correspond to the original (unrotated) image dimensions.
 */
export function transformFaceBox(
  box: FaceBoxDto,
  orientation: number | undefined,
  width: number,
  height: number
): FaceBoxDto {
  switch (orientation) {
    case 6: // Rotate 90°
      return {
        left: box.top,
        top: width - box.left - box.width,
        width: box.height,
        height: box.width,
      };
    case 8: // Rotate 270°
      return {
        left: height - box.top - box.height,
        top: box.left,
        width: box.height,
        height: box.width,
      };
    case 3: // Rotate 180°
      return {
        left: width - box.left - box.width,
        top: height - box.top - box.height,
        width: box.width,
        height: box.height,
      };
    default:
      return box;
  }
}

