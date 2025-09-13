import type { MyContext } from "../i18n";
import { getUser } from "../services/auth";
import { handleCommandError } from "../errorHandler";

export async function profileCommand(ctx: MyContext) {
    const username = ctx.from?.username ?? String(ctx.from?.id ?? "");
    try {
        await getUser(ctx);
        await ctx.reply(ctx.t('user-info', { username }));
    } catch (error: unknown) {
        if (error instanceof Error && error.message.includes('404')) {
            await ctx.reply(ctx.t('not-registered', { userId: ctx.from?.id ?? 0 }));
            return;
        }
        await handleCommandError(ctx, error);
        await ctx.reply(ctx.t('get-profile-error'));
    }
}
