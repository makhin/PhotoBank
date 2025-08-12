import { Context } from "grammy";
import {
    authGetUser,
    authGetUserRoles,
    authGetUserClaims,
} from "@photobank/shared/src/api/photobank";
import {
    apiErrorMsg,
    getProfileErrorMsg,
    rolesLabel,
    rolesEmptyLabel,
    claimsLabel,
    claimsEmptyLabel,
    notRegisteredMsg,
} from "@photobank/shared/constants";
import { logger } from "../logger";

export async function profileCommand(ctx: Context) {
    const username = ctx.from?.username ?? String(ctx.from?.id ?? "");
    try {
        await authGetUser();
        const [rolesRes, claimsRes] = await Promise.all([
            authGetUserRoles(),
            authGetUserClaims(),
        ]);
        const roles = rolesRes.data;
        const claims = claimsRes.data;

        const lines: string[] = [
            `👤 Пользователь: ${username}`,
        ];

        if (roles.length) {
            lines.push(rolesLabel);
            for (const role of roles) {
                lines.push(`- ${role.name}`);
                for (const claim of role.claims) {
                    lines.push(`  • ${claim.type}: ${claim.value}`);
                }
            }
        } else {
            lines.push(rolesEmptyLabel);
        }

        if (claims.length) {
            lines.push(claimsLabel);
            for (const claim of claims) {
                lines.push(`- ${claim.type}: ${claim.value}`);
            }
        } else {
            lines.push(claimsEmptyLabel);
        }

        await ctx.reply(lines.join("\n"));
    } catch (error: any) {
        if (error instanceof Error && error.message.includes('404')) {
            await ctx.reply(notRegisteredMsg);
            return;
        }
        logger.error(apiErrorMsg, error);
        await ctx.reply(getProfileErrorMsg);
    }
}
