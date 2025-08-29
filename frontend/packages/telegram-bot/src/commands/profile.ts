import type { MyContext } from "../i18n";
import { getUser, getUserRoles, getUserClaims } from "../services/auth";
import { handleCommandError } from "../errorHandler";

export async function profileCommand(ctx: MyContext) {
    const username = ctx.from?.username ?? String(ctx.from?.id ?? "");
    try {
        await getUser(ctx);
        const [rolesRes, claimsRes] = await Promise.all([
            getUserRoles(ctx),
            getUserClaims(ctx),
        ]);
        const roles = rolesRes.data;
        const claims = claimsRes.data;

        const lines: string[] = [
            ctx.t('user-info', { username }),
        ];

        if (roles.length) {
            lines.push(ctx.t('roles-label'));
            for (const role of roles) {
                lines.push(`- ${role.name}`);
                for (const claim of role.claims) {
                    lines.push(`  • ${claim.type}: ${claim.value}`);
                }
            }
        } else {
            lines.push(ctx.t('roles-empty'));
        }

        if (claims.length) {
            lines.push(ctx.t('claims-label'));
            for (const claim of claims) {
                lines.push(`- ${claim.type}: ${claim.value}`);
            }
        } else {
            lines.push(ctx.t('claims-empty'));
        }

        await ctx.reply(lines.join("\n"));
    } catch (error: unknown) {
        if (error instanceof Error && error.message.includes('404')) {
            await ctx.reply(ctx.t('not-registered', { userId: ctx.from?.id }));
            return;
        }
        await handleCommandError(ctx, error);
        await ctx.reply(ctx.t('get-profile-error'));
    }
}
