import { Context } from "grammy";
import { AuthService } from "@photobank/shared/generated";
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
        await AuthService.getApiAuthUser();
        const [roles, claims] = await Promise.all([
            AuthService.getApiAuthRoles(),
            AuthService.getApiAuthClaims(),
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
    } catch (error: any) {
        if (error?.response?.status === 404) {
            await ctx.reply(notRegisteredMsg);
            return;
        }
        logger.error(apiErrorMsg, error);
        await ctx.reply(getProfileErrorMsg);
    }
}
