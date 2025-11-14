# TV Package Scripts

## setup-symlinks.sh

Скрипт для создания необходимых symlink'ов для React Native зависимостей в pnpm монорепозитории.

### Проблема

В pnpm монорепозитории Gradle-плагины React Native модулей (например, async-storage) ищут `@react-native/codegen` в локальном `node_modules` пакета, но из-за hoisting он находится в корневом `node_modules` монорепозитория.

### Решение

Скрипт создает symlink из `packages/tv/node_modules/@react-native/codegen` в `frontend/node_modules/@react-native/codegen`.

### Использование

#### Автоматически (рекомендуется)

Symlink создается автоматически после установки зависимостей через postinstall хук:

```bash
# Из корня frontend/
pnpm install

# Или только для tv пакета
pnpm --filter @photobank/tv install
```

#### Вручную

```bash
# Из packages/tv/
pnpm run setup-symlinks

# Или напрямую
bash scripts/setup-symlinks.sh
```

### Когда нужно запускать

- После первой установки зависимостей
- После переустановки зависимостей (`pnpm install`)
- После очистки node_modules
- Если возникает ошибка: "Cannot find module '@react-native/codegen/lib/cli/combine/combine-js-to-schema-cli.js'"

### Проверка

Скрипт автоматически проверяет корректность созданного symlink и выведет:
- `✓ Symlink created successfully!` - если все в порядке
- `✗ Symlink verification failed` - если что-то пошло не так
