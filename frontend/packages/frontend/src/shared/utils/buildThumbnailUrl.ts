import type { PhotoItemDto } from '@/shared/types'; // подкорректируй импорт под свой путь

export function buildThumbnailUrl(photo: PhotoItemDto): string {
  // 1) если API уже прислал готовый URL — используем его
  const anyPhoto = photo as any;
  if (anyPhoto.thumbnailUrl) return String(anyPhoto.thumbnailUrl);

  // 2) иначе собираем сами
  const base =
    import.meta.env.VITE_S3_PUBLIC_BASE_URL ||
    import.meta.env.VITE_API_BASE_URL ||
    '';

  const storage = (anyPhoto.storageName ?? anyPhoto.storage ?? '').toString();
  const rel = (anyPhoto.relativePath ?? '').toString();
  const file = (anyPhoto.fileName ?? '').toString();

  const parts = [storage, rel, `${file}_thumbnail.jpg`]
    .filter(Boolean)
    .map((x) => encodeURIComponent(x));

  return [base.replace(/\/+$/, ''), parts.join('/')].filter(Boolean).join('/');
}