import { Context } from "grammy";
import { getUserClaims } from "@photobank/shared/api";

export async function claimsCommand(ctx: Context) {
    try {
        const claims = await getUserClaims();
        if (!claims.length) {
            await ctx.reply("Нет данных о правах пользователя.");
            return;
        }
        const lines = claims.map(c => `${c.type}: ${c.value}`);
        await ctx.reply(lines.join("\n"));
    } catch (error) {
        console.error("Ошибка при получении прав:", error);
        await ctx.reply("🚫 Не удалось получить список прав.");
    }
}
