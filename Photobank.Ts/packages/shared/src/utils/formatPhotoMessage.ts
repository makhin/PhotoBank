import { PhotoDto } from "@photobank/shared/types";
import { getPersonName } from "@photobank/shared/dictionaries";
import { formatDate } from "@photobank/shared/index";
import { Buffer } from "buffer";

export function formatPhotoMessage(photo: PhotoDto): { caption: string, image?: Buffer } {
    const lines: string[] = [];

    lines.push(`📸 <b>${photo.name ?? "Без названия"}</b>`);
    if (photo.takenDate) {
        lines.push(`📅 ${formatDate(photo.takenDate)}`);
    }

    // lines.push(`📏 ${photo.width}×${photo.height}`);

    if (photo.captions?.[0]) lines.push(`📝 ${photo.captions[0]}`);
    if (photo.tags?.length) lines.push(`🏷️ ${photo.tags.join(", ")}`);

    if (photo.faces?.length) {
        const people = photo.faces
            .map(f => f.personId ?? 0)
            .filter(Boolean)
            .map(getPersonName);
        if (people.length) {
            lines.push(`👤 ${people.join(", ")}`);
        }
    }

    const image = photo.previewImage
        ? Buffer.from(photo.previewImage, "base64")
        : undefined;

    return {
        caption: lines.join("\n"),
        image,
    };
}
