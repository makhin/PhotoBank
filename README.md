# PhotoBank

Проект состоит из нескольких модулей:

- **PhotoBank.Api** – ASP.NET Core веб‑API.
- **Photobank.Ts** – monorepo на Node.js (frontend, telegram‑bot и общие библиотеки).
- **PhotoBank.MAUI.Blazor** – кросс‑платформенное приложение на .NET MAUI.
- **Photobank.ServerBlazorApp** – серверное Blazor‑приложение.
- **PhotoBank.Console** – консольные утилиты.
- **PhotoBank.DbContext**, **PhotoBank.Repositories**, **PhotoBank.Services**, **PhotoBank.ViewModel.Dto** – общие слои приложения.
- **PhotoBank.UnitTests**, **PhotoBank.IntegrationTests** – проекты с тестами.

## Сборка и запуск

### Backend

```bash
# восстановить зависимости и запустить API
dotnet restore
dotnet run --project PhotoBank.Api
```

Для работы API нужны настройки подключения к БД и параметры JWT. Их можно задать в `appsettings.json` или через переменные окружения:

```bash
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection="Server=localhost;Database=Photobank;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False;"
Jwt__Key="SuperSecretKey1234567890"
Jwt__Issuer="PhotoBank.Api"
Jwt__Audience="PhotoBank.Api"
```

### Фронтенд

```bash
cd Photobank.Ts
pnpm install
pnpm dev
```

Перед запуском задайте `VITE_API_BASE_URL` (или в `.env` файл) – адрес API, например:

```bash
VITE_API_BASE_URL=http://localhost:5066
```

### Телеграм‑бот

```bash
cd Photobank.Ts
pnpm install
pnpm bot
```

Боту нужны переменные окружения (можно оформить в `.env`):

```bash
BOT_TOKEN=ваш_токен
API_EMAIL=admin@example.com
API_PASSWORD=secret
API_BASE_URL=http://localhost:5066
```

### MAUI/Blazor клиент

```bash
dotnet build PhotoBank.MAUI.Blazor/PhotoBank.MAUI.Blazor.csproj
# для запуска на десктопе
dotnet run --project PhotoBank.MAUI.Blazor
```

### Тесты

Для .NET проектов:

```bash
dotnet test PhotoBank.sln
```

Для пакетов Node.js:

```bash
cd Photobank.Ts
pnpm -r test
```

## Лицензия

Проект распространяется под лицензией MIT. Текст лицензии находится в файле [LICENSE](LICENSE).

---

# PhotoBank (English)

The project is composed of several modules:

- **PhotoBank.Api** – ASP.NET Core web API.
- **Photobank.Ts** – Node.js monorepo (frontend, Telegram bot and shared libraries).
- **PhotoBank.MAUI.Blazor** – cross-platform .NET MAUI app.
- **Photobank.ServerBlazorApp** – server-side Blazor application.
- **PhotoBank.Console** – command-line utilities.
- **PhotoBank.DbContext**, **PhotoBank.Repositories**, **PhotoBank.Services**, **PhotoBank.ViewModel.Dto** – common layers.
- **PhotoBank.UnitTests**, **PhotoBank.IntegrationTests** – test projects.

## Build and run

### Backend

```bash
dotnet restore
dotnet run --project PhotoBank.Api
```

The API requires database connection settings and JWT parameters configured either in `appsettings.json` or via environment variables:

```bash
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection="Server=localhost;Database=Photobank;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False;"
Jwt__Key="SuperSecretKey1234567890"
Jwt__Issuer="PhotoBank.Api"
Jwt__Audience="PhotoBank.Api"
```

### Frontend

```bash
cd Photobank.Ts
pnpm install
pnpm dev
```

Before running set `VITE_API_BASE_URL` (or put it into `.env`) – the API address, for example:

```bash
VITE_API_BASE_URL=http://localhost:5066
```

### Telegram bot

```bash
cd Photobank.Ts
pnpm install
pnpm bot
```

The bot requires environment variables (can be placed in `.env`):

```bash
BOT_TOKEN=your_token
API_EMAIL=admin@example.com
API_PASSWORD=secret
API_BASE_URL=http://localhost:5066
```

### MAUI/Blazor client

```bash
dotnet build PhotoBank.MAUI.Blazor/PhotoBank.MAUI.Blazor.csproj
dotnet run --project PhotoBank.MAUI.Blazor
```

### Tests

For .NET projects:

```bash
dotnet test PhotoBank.sln
```

For Node.js packages:

```bash
cd Photobank.Ts
pnpm -r test
```

## License

The project is distributed under the MIT license. See [LICENSE](LICENSE).
