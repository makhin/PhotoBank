import { Context } from "grammy";
import { todaysPhotosEmptyMsg } from "@photobank/shared/constants";

import { sendPhotosPage } from "./photosPage";

function parsePage(text?: string): number {
    if (!text) return 1;
    const match = text.match(/\/thisday\s+(\d+)/);
    return match ? parseInt(match[1], 10) || 1 : 1;
}

export async function handleThisDay(ctx: Context) {
    const page = parsePage(ctx.message?.text);
    await sendThisDayPage(ctx, page);
}

export const thisDayCommand = handleThisDay;
export async function sendThisDayPage(ctx: Context, page: number, edit = false) {
    await sendPhotosPage({
        ctx,
        filter: { thisDay: true },
        page,
        edit,
        fallbackMessage: todaysPhotosEmptyMsg,
        buildCallbackData: (p) => `thisday:${p}`,
    });
}
