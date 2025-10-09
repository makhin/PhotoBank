import {
  formatDate,
  formatGeoLink,
  getPlaceByGeoPoint,
  isValidGeoPoint,
  type PhotoDto,
} from '@photobank/shared';

import { getCurrentLocale, getPersonName } from './dictionaries';
import { i18n } from './i18n';

export async function formatPhotoMessage(photo: PhotoDto): Promise<{
  caption: string;
  hasSpoiler: boolean;
  imageUrl?: string;
}> {
  const lines: string[] = [];

  lines.push(`📸 <b>${photo.name}</b>`);
  const takenDate = formatDate(photo.takenDate);
  if (takenDate) {
    lines.push(i18n.t(getCurrentLocale(), 'photo-date-label', { date: takenDate }));
  }

  // lines.push(`📏 ${photo.width}×${photo.height}`);

  if (photo.captions?.[0]) lines.push(`📝 ${photo.captions[0]}`);
  if (photo.tags?.length) lines.push(`🏷️ ${photo.tags.join(", ")}`);

  if (isValidGeoPoint(photo.location)) {
    const point = { latitude: photo.location.latitude, longitude: photo.location.longitude };
    const placeName = await getPlaceByGeoPoint(point);
    const { href, label } = formatGeoLink(point, placeName);
    lines.push(`📍 <a href="${href}">${label}</a>`);
  }

  if (photo.faces?.length) {
    const people = photo.faces.map((f: { personId?: number | null }) => getPersonName(f.personId ?? null));
    if (people.some(Boolean)) {
      lines.push(`👤 ${people.join(", ")}`);
    }
  }

  const imageUrl = photo.previewUrl ?? undefined;

  return {
    caption: lines.join("\n"),
    hasSpoiler: (photo.adultScore ?? 0) > 0.5 || (photo.racyScore ?? 0) > 0.5,
    ...(imageUrl ? { imageUrl } : {}),
  };
}
