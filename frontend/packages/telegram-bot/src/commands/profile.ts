import { Context } from "grammy";
import { getUserRoles, getUserClaims } from "@photobank/shared/api";
import {
    apiErrorMsg,
    getProfileErrorMsg,
    rolesLabel,
    rolesEmptyLabel,
    claimsLabel,
    claimsEmptyLabel,
} from "@photobank/shared/constants";

export async function profileCommand(ctx: Context) {
    const username = ctx.from?.username ?? String(ctx.from?.id ?? "");
    try {
        const [roles, claims] = await Promise.all([
            getUserRoles(),
            getUserClaims(),
        ]);

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
    } catch (error) {
        console.error(apiErrorMsg, error);
        await ctx.reply(getProfileErrorMsg);
    }
}
