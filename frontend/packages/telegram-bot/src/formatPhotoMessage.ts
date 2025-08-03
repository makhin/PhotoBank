import { PhotoDto } from "@photobank/shared/generated";
import { getPersonName } from "./dictionaries";
import { formatDate } from "@photobank/shared/index";
import { Buffer } from "buffer";

export function formatPhotoMessage(photo: PhotoDto): { caption: string, hasSpoiler: boolean, image?: Buffer } {
    const lines: string[] = [];

    lines.push(`📸 <b>${photo.name}</b>`);
    if (photo.takenDate) {
        lines.push(`📅 ${formatDate(photo.takenDate)}`);
    }

    // lines.push(`📏 ${photo.width}×${photo.height}`);

    if (photo.captions?.[0]) lines.push(`📝 ${photo.captions[0]}`);
    if (photo.tags?.length) lines.push(`🏷️ ${photo.tags.join(", ")}`);

    if (photo.faces?.length) {
        const people = photo.faces.map(f => getPersonName(f.personId));
        if (people.some(Boolean)) {
            lines.push(`👤 ${people.join(", ")}`);
        }
    }

    const image = photo.previewImage
        ? Buffer.from(photo.previewImage, "base64")
        : undefined;

    return {
        caption: lines.join("\n"),
        hasSpoiler: photo.adultScore > 0.5 || photo.racyScore > 0.5,
        image,
    };
}
