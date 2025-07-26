import { Context, InlineKeyboard } from "grammy";
import { getAllPersons } from "@photobank/shared/api";
import { prevPageText, nextPageText } from "@photobank/shared/constants";

const PAGE_SIZE = 10;

function parsePrefix(text?: string): string {
    if (!text) return "";
    const parts = text.split(" ");
    return parts[1]?.trim() ?? "";
}

export async function sendPersonsPage(ctx: Context, prefix: string, page: number, edit = false) {
    let persons;
    try {
        persons = await getAllPersons();
    } catch (err) {
        console.error(err);
        await ctx.reply("🚫 Не удалось получить список персон.");
        return;
    }

    const filtered = persons
        .filter(p => p.name.toLowerCase().startsWith(prefix.toLowerCase()))
        .sort((a, b) => a.name.localeCompare(b.name));

    const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
    const pageIndex = Math.min(Math.max(page, 1), totalPages);
    const items = filtered.slice((pageIndex - 1) * PAGE_SIZE, pageIndex * PAGE_SIZE);

    const lines = items.map(p => `- ${p.name}`);
    lines.push("", `📄 Страница ${pageIndex} из ${totalPages}`);

    const keyboard = new InlineKeyboard();
    if (pageIndex > 1) keyboard.text(prevPageText, `persons:${pageIndex - 1}:${encodeURIComponent(prefix)}`);
    if (pageIndex < totalPages) keyboard.text(nextPageText, `persons:${pageIndex + 1}:${encodeURIComponent(prefix)}`);

    const text = lines.join("\n");

    if (edit) {
        await ctx.editMessageText(text, { reply_markup: keyboard }).catch(() =>
            ctx.reply(text, { reply_markup: keyboard })
        );
    } else {
        await ctx.reply(text, { reply_markup: keyboard });
    }
}

export async function personsCommand(ctx: Context) {
    const prefix = parsePrefix(ctx.message?.text);
    await sendPersonsPage(ctx, prefix, 1);
}

