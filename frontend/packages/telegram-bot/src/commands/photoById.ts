import { Context, InputFile } from "grammy";
import { getPhotoById } from "@photobank/shared/api";
import { formatPhotoMessage } from "@photobank/shared/utils/formatPhotoMessage";
import {
    apiErrorMsg,
    getPhotoErrorMsg,
    photoCommandUsageMsg,
    photoNotFoundMsg,
} from "@photobank/shared/constants";

export async function photoByIdCommand(ctx: Context) {
    const parts = ctx.message?.text?.split(" ");
    const id = Number(parts?.[1]);

    if (!id || isNaN(id)) {
        await ctx.reply(photoCommandUsageMsg);
        return;
    }

    try {
        const photo = await getPhotoById(id);

        if (!photo) {
            await ctx.reply(photoNotFoundMsg);
            return;
        }

        const { caption, hasSpoiler, image } = formatPhotoMessage(photo);

        if (image) {
            const file = new InputFile(image, `${photo.name}.jpg`);
            await ctx.replyWithPhoto(file, { caption, parse_mode: "HTML", has_spoiler: hasSpoiler });
        } else {
            await ctx.reply(caption, { parse_mode: "HTML" });
        }
    } catch (error) {
        console.error(apiErrorMsg, error);
        await ctx.reply(getPhotoErrorMsg);
    }
}
