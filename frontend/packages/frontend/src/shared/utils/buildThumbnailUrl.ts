import { type PhotoItemDto } from '@photobank/shared/api/photobank';
import { env } from '@/env';

export function buildThumbnailUrl(photo: PhotoItemDto): string {
  if (photo.thumbnailUrl) return String(photo.thumbnailUrl);

  // 2) иначе собираем сами
  const base =
    env.VITE_S3_PUBLIC_BASE_URL ||
    env.VITE_API_BASE_URL ||
    '';

  const storage = (photo.storageName  ?? '').toString();
  const rel = (photo.relativePath ?? '').toString();
  const file = (photo.id ?? '').toString();

  const parts = [storage, rel, `${file}_thumbnail.jpg`]
    .filter(Boolean)
    .map((x) => encodeURIComponent(x));

  return [base.replace(/\/+$/, ''), parts.join('/')].filter(Boolean).join('/');
}
