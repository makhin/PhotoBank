import { Context } from "grammy";
import {
    getProfileErrorMsg,
    rolesLabel,
    rolesEmptyLabel,
    claimsLabel,
    claimsEmptyLabel,
    notRegisteredMsg,
} from "@photobank/shared/constants";

import { getUser, getUserRoles, getUserClaims } from "../services/auth";
import { handleCommandError } from "../errorHandler";

export async function profileCommand(ctx: Context) {
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
    } catch (error: unknown) {
        if (error instanceof Error && error.message.includes('404')) {
            await ctx.reply(notRegisteredMsg);
            return;
        }
        await handleCommandError(ctx, error);
        await ctx.reply(getProfileErrorMsg);
    }
}
