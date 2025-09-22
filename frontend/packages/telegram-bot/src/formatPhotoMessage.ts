import { formatDate, type PhotoDto } from "@photobank/shared";

import { getPersonName } from "./dictionaries";
import { escapeHtml } from "./utils/escapeHtml";

export function formatPhotoMessage(photo: PhotoDto): { caption: string; hasSpoiler: boolean; imageUrl?: string } {
  const lines: string[] = [];

  const name = escapeHtml(photo.name ?? "");
  lines.push(`📸 <b>${name}</b>`);
  if (photo.takenDate) {
    lines.push(`📅 ${escapeHtml(formatDate(photo.takenDate))}`);
  }

  // lines.push(`📏 ${photo.width}×${photo.height}`);

  const firstCaption = photo.captions?.[0];
  if (firstCaption) {
    lines.push(`📝 ${escapeHtml(firstCaption)}`);
  }
  if (photo.tags?.length) {
    const safeTags = photo.tags.map((tag) => escapeHtml(tag)).join(", ");
    lines.push(`🏷️ ${safeTags}`);
  }

  if (photo.location) {
    const { latitude, longitude } = photo.location;
    const coords = `${latitude.toFixed(5)}, ${longitude.toFixed(5)}`;
    const safeCoords = escapeHtml(coords);
    const mapUrl = `https://www.google.com/maps?q=${encodeURIComponent(latitude)},${encodeURIComponent(longitude)}`;
    lines.push(`📍 <a href="${escapeHtml(mapUrl)}">${safeCoords}</a>`);
  }

  if (photo.faces?.length) {
    const people = photo.faces
      .map((f: { personId?: number | null }) => getPersonName(f.personId ?? null))
      .map((personName) => personName.trim())
      .filter((personName) => personName.length > 0)
      .map((personName) => escapeHtml(personName));
    if (people.length) {
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
