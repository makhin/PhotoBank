import { Context } from 'grammy';
import {
  aiCommandUsageMsg,
  sorryTryToRequestLaterMsg,
} from '@photobank/shared/constants';
import { createChatCompletion } from '@photobank/shared/ai/openai';

export function parseAiPrompt(text?: string): string | null {
    if (!text) return null;
    const match = text.match(/^\/ai\s+([\s\S]+)/); // capture anything after /ai
    if (!match) return null;
    return match[1].trim();
}

export async function aiCommand(ctx: Context) {
  const prompt = parseAiPrompt(ctx.message?.text);
  if (!prompt) {
    await ctx.reply(aiCommandUsageMsg);
    return;
  }
  try {
    const response = await createChatCompletion(prompt);
        await ctx.reply(response ?? sorryTryToRequestLaterMsg);
  } catch (err) {
    console.error(err);
    await ctx.reply(sorryTryToRequestLaterMsg);
  }
}
