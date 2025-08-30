import { InputFile } from 'grammy';

export function toTelegramFile(src?: string): string | InputFile | undefined {
    if (!src) return undefined;
    if (/^https?:\/\//i.test(src)) return src;
    try {
        return new InputFile(Buffer.from(src, 'base64'));
    } catch {
        return undefined;
    }
}
