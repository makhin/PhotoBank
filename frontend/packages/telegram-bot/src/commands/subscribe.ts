import { Bot, Context } from "grammy";
import { sendThisDayPage } from "./thisday";
import { subscribeCommandUsageMsg } from "@photobank/shared/constants";

export const subscriptions = new Map<number, string>();

export function parseSubscribeTime(text?: string): string | null {
    if (!text) return null;
    const match = text.match(/\/subscribe\s+(\d{1,2}:\d{2})/);
    if (!match) return null;
    const [hours, minutes] = match[1].split(":").map(Number);
    if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59) return null;
    return `${hours.toString().padStart(2, "0")}:${minutes.toString().padStart(2, "0")}`;
}

export async function subscribeCommand(ctx: Context) {
    const time = parseSubscribeTime(ctx.message?.text);
    if (!time) {
        await ctx.reply(subscribeCommandUsageMsg);
        return;
    }
    subscriptions.set(ctx.chat.id, time);
    await ctx.reply(`✅ Подписка на ежедневную рассылку в ${time} оформлена.`);
}

export function initSubscriptionScheduler(bot: Bot) {
    setInterval(async () => {
        const now = new Date();
        const current = `${String(now.getHours()).padStart(2, "0")}:${String(now.getMinutes()).padStart(2, "0")}`;
        for (const [chatId, t] of subscriptions.entries()) {
            if (t === current) {
                const ctxLike = {
                    message: { text: "/thisday" },
                    reply: (text: string, opts?: any) => bot.api.sendMessage(chatId, text, opts),
                } as unknown as Context;
                await sendThisDayPage(ctxLike, 1);
            }
        }
    }, 60 * 1000);
}
