import { Context, InlineKeyboard } from "grammy";
import { getAllTags } from "@photobank/shared/api";
import { prevPageText, nextPageText } from "@photobank/shared/constants";

const PAGE_SIZE = 10;

function parsePrefix(text?: string): string {
    if (!text) return "";
    const parts = text.split(" ");
    return parts[1]?.trim() ?? "";
}

export async function sendTagsPage(ctx: Context, prefix: string, page: number, edit = false) {
    let tags;
    try {
        tags = await getAllTags();
    } catch (err) {
        console.error(err);
        await ctx.reply("🚫 Не удалось получить список тегов.");
        return;
    }

    const filtered = tags
        .filter(t => t.name.toLowerCase().startsWith(prefix.toLowerCase()))
        .sort((a, b) => a.name.localeCompare(b.name));

    const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
    const pageIndex = Math.min(Math.max(page, 1), totalPages);
    const items = filtered.slice((pageIndex - 1) * PAGE_SIZE, pageIndex * PAGE_SIZE);

    const lines = items.map(t => `- ${t.name}`);
    lines.push("", `📄 Страница ${pageIndex} из ${totalPages}`);

    const keyboard = new InlineKeyboard();
    if (pageIndex > 1) keyboard.text(prevPageText, `tags:${pageIndex - 1}:${encodeURIComponent(prefix)}`);
    if (pageIndex < totalPages) keyboard.text(nextPageText, `tags:${pageIndex + 1}:${encodeURIComponent(prefix)}`);

    const text = lines.join("\n");

    if (edit) {
        await ctx.editMessageText(text, { reply_markup: keyboard }).catch(() =>
            ctx.reply(text, { reply_markup: keyboard })
        );
    } else {
        await ctx.reply(text, { reply_markup: keyboard });
    }
}

export async function tagsCommand(ctx: Context) {
    const prefix = parsePrefix(ctx.message?.text);
    await sendTagsPage(ctx, prefix, 1);
}

