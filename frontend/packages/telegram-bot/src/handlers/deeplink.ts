import { bot } from '../bot';

bot.on('message', async (ctx, next) => {
  const text = ctx.message?.text ?? '';
  const m = /^\/start(?:\s+(\S+))?/.exec(text);
  if (m) {
    const param = m[1];
    if (param === 'link') {
      await ctx.reply('Ваш Telegram не привязан. Напишите администратору для привязки.');
      return;
    }
    if (param === 'help') {
      await ctx.reply('Пример запроса в inline‑режиме: @имябота котики, @имябота дата:2024');
      return;
    }
  }
  return next();
});
