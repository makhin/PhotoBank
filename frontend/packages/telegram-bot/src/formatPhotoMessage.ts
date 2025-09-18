import { formatDate, type PhotoDto } from "@photobank/shared";

import { getPersonName } from "./dictionaries";

export function formatPhotoMessage(photo: PhotoDto): { caption: string; hasSpoiler: boolean; imageUrl?: string } {
  const lines: string[] = [];

  lines.push(`📸 <b>${photo.name}</b>`);
  if (photo.takenDate) {
    lines.push(`📅 ${formatDate(photo.takenDate)}`);
  }

  // lines.push(`📏 ${photo.width}×${photo.height}`);

  if (photo.captions?.[0]) lines.push(`📝 ${photo.captions[0]}`);
  if (photo.tags?.length) lines.push(`🏷️ ${photo.tags.join(", ")}`);

  if (photo.location) {
    const { latitude, longitude } = photo.location;
    const coords = `${latitude.toFixed(5)}, ${longitude.toFixed(5)}`;
    lines.push(`📍 <a href="https://www.google.com/maps?q=${latitude},${longitude}">${coords}</a>`);
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
