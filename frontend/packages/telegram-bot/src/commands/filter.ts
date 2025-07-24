import { Context, InlineKeyboard } from "grammy";
import { searchPhotos } from "@photobank/shared/api/photos";
import type { FilterDto } from "@photobank/shared/types";
import {
    prevPageText,
    nextPageText,
    sorryTryToRequestLaterMsg,
    photoNotFoundMsg,
    apiErrorMsg,
} from "@photobank/shared/constants";
import { sendPhotoById } from "../photo";

interface WizardState {
    step: number;
    filter: FilterDto;
}

const wizards = new Map<number, WizardState>();

interface ResultState {
    ids: number[];
    index: number;
}

const results = new Map<number, ResultState>();

export async function filterCommand(ctx: Context) {
    wizards.set(ctx.chat.id, { step: 1, filter: {} });
    await ctx.reply("Введите ID хранилища:");
}

export async function handleFilterWizard(ctx: Context) {
    const state = wizards.get(ctx.chat.id);
    if (!state || !ctx.message?.text) return false;
    const text = ctx.message.text.trim();

    switch (state.step) {
        case 1:
            state.filter.storages = text ? [Number(text)] : undefined;
            state.step = 2;
            await ctx.reply("Введите дату YYYY-MM-DD:");
            break;
        case 2:
            state.filter.takenDateFrom = text || undefined;
            state.filter.takenDateTo = text || undefined;
            state.step = 3;
            await ctx.reply("Введите ID персоны:");
            break;
        case 3:
            state.filter.persons = text ? [Number(text)] : undefined;
            state.step = 4;
            await ctx.reply("Введите ID тега:");
            break;
        case 4:
            state.filter.tags = text ? [Number(text)] : undefined;
            state.step = 5;
            await ctx.reply("Контент для взрослых? (yes/no)");
            break;
        case 5:
            state.filter.isAdultContent = /^y(es)?|1|да$/i.test(text);
            wizards.delete(ctx.chat.id);
            await executeFilter(ctx, state.filter);
            break;
    }
    return true;
}

async function executeFilter(ctx: Context, filter: FilterDto) {
    let queryResult;
    try {
        queryResult = await searchPhotos(filter);
    } catch (err) {
        console.error(apiErrorMsg, err);
        await ctx.reply(sorryTryToRequestLaterMsg);
        return;
    }

    if (!queryResult.photos?.length) {
        await ctx.reply(photoNotFoundMsg);
        return;
    }

    const ids = queryResult.photos.map(p => p.id);
    results.set(ctx.chat.id, { ids, index: 0 });
    await sendFilteredPhoto(ctx, ctx.chat.id, 0);
}

async function sendFilteredPhoto(ctx: Context, chatId: number, index: number) {
    const state = results.get(chatId);
    if (!state) return;
    state.index = index;
    const id = state.ids[index];
    await sendPhotoById(ctx, id);
    const keyboard = new InlineKeyboard();
    if (index > 0) keyboard.text(prevPageText, `filter_prev:${index - 1}`);
    if (index < state.ids.length - 1) keyboard.text(nextPageText, `filter_next:${index + 1}`);
    await ctx.reply("Переключайте фото:", { reply_markup: keyboard });
}

export async function handleFilterNavigation(ctx: Context) {
    const match = ctx.callbackQuery?.data?.match(/^filter_(next|prev):(\d+)$/);
    if (!match) return false;
    const index = Number(match[2]);
    await ctx.answerCallbackQuery();
    await sendFilteredPhoto(ctx, ctx.chat.id, index);
    return true;
}
