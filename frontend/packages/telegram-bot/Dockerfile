FROM node:20-alpine AS build
WORKDIR /app

# Копируем lock-файл и package.json
COPY pnpm-lock.yaml package.json ./

RUN corepack enable && pnpm install --frozen-lockfile

# Копируем код пакета
COPY packages/telegram-bot ./packages/telegram-bot

# Production слой
FROM node:20-alpine AS production
WORKDIR /app

COPY --from=build /app/packages/telegram-bot ./packages/telegram-bot
COPY --from=build /app/node_modules ./node_modules
COPY package.json ./

CMD ["pnpm", "--filter", "telegram-bot", "start"]
