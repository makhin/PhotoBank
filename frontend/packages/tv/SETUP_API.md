# Настройка API интеграции с PhotoBank

## Что было добавлено

### 1. Зависимости
- `axios@^1.7.9` - HTTP клиент
- `@tanstack/react-query@^5.90.3` - Управление состоянием сервера
- `orval@^7.13.2` - Генератор TypeScript кода из OpenAPI

### 2. Структура файлов

```
src/api/
├── client.ts              # Axios instance с interceptors для токена
├── queryClient.ts         # Конфигурация React Query
├── index.ts               # Экспорты всех типов и хуков
├── README.md              # Детальная документация по использованию
└── generated/             # Автогенерированный код (не редактировать!)
    ├── photoBankApi.schemas.ts  # TypeScript типы
    ├── auth/              # Хуки аутентификации
    ├── photos/            # Хуки для работы с фото
    ├── persons/           # Хуки для персон
    ├── person-groups/     # Хуки для групп персон
    ├── faces/             # Хуки для лиц
    ├── users/             # Хуки для пользователей
    ├── admin-access-profiles/  # Хуки для профилей доступа
    ├── storages/          # Хуки для хранилищ
    ├── tags/              # Хуки для тегов
    └── version/           # Хуки для версии API
```

### 3. Конфигурационные файлы

- `orval.config.ts` - Настройка генерации кода
- Обновлён `package.json` - добавлены скрипты `generate:api` и `generate:api:watch`
- Обновлён `tsconfig.json` - добавлен alias `@api/*` и изменён `moduleResolution` на `bundler`
- Обновлён `babel.config.js` - добавлен alias `@api`
- Обновлён `App.tsx` - добавлен `QueryClientProvider`

## Как использовать

### 1. Настройте базовый URL API

Отредактируйте `src/api/client.ts:9`:

```typescript
export const axiosInstance = axios.create({
  baseURL: 'https://your-actual-api.com/api', // Замените на ваш URL
  // ...
});
```

### 2. Регенерация API после изменений OpenAPI

Если `openapi.yaml` изменился, запустите:

```bash
pnpm run generate:api
```

### 3. Примеры использования в компонентах

#### Пример: Логин

```typescript
import { useAuthLogin, authHelpers } from '@api';

const LoginScreen = () => {
  const loginMutation = useAuthLogin({
    mutation: {
      onSuccess: async (response) => {
        if (response.token) {
          await authHelpers.saveToken(response.token);
          // Навигация или обновление состояния
        }
      },
    },
  });

  const handleLogin = () => {
    loginMutation.mutate({
      data: {
        email: 'user@example.com',
        password: 'password123',
        rememberMe: true,
      },
    });
  };

  return (
    <Button
      title={loginMutation.isPending ? 'Loading...' : 'Login'}
      onPress={handleLogin}
    />
  );
};
```

#### Пример: Получение текущего пользователя

```typescript
import { useAuthGetUser } from '@api';

const ProfileScreen = () => {
  const { data: user, isLoading, error } = useAuthGetUser();

  if (isLoading) return <LoadingSpinner />;
  if (error) return <ErrorMessage />;

  return <Text>Email: {user?.email}</Text>;
};
```

#### Пример: Поиск фотографий

```typescript
import { usePhotosSearchPhotos, type FilterDto } from '@api';

const GalleryScreen = () => {
  const searchMutation = usePhotosSearchPhotos();

  useEffect(() => {
    const filters: FilterDto = {
      page: 1,
      pageSize: 20,
      storages: [1],
    };
    searchMutation.mutate({ data: filters });
  }, []);

  return (
    <FlatList
      data={searchMutation.data?.items || []}
      renderItem={({ item }) => <PhotoCard photo={item} />}
    />
  );
};
```

## Доступные API endpoints

### Аутентификация (Auth)
- `useAuthLogin` - Вход в систему
- `useAuthRegister` - Регистрация
- `useAuthGetUser` - Получить текущего пользователя
- `useAuthUpdateUser` - Обновить профиль
- `useAuthGetTelegramSubscriptions` - Telegram подписки
- `useAuthTelegramExchange` - Обмен Telegram токена

### Фотографии (Photos)
- `usePhotosSearchPhotos` - Поиск фото с фильтрами
- `usePhotosGetPhoto` - Получить фото по ID
- `usePhotosUpload` - Загрузить фото
- `usePhotosGetDuplicates` - Найти дубликаты

### Персоны (Persons)
- `usePersonsGetAll` - Получить всех персон
- `usePersonsCreate` - Создать персону
- `usePersonsUpdate` - Обновить персону
- `usePersonsDelete` - Удалить персону

### Группы персон (PersonGroups)
- `usePersonGroupsGetAll` - Получить все группы
- `usePersonGroupsCreate` - Создать группу
- `usePersonGroupsUpdate` - Обновить группу
- `usePersonGroupsDelete` - Удалить группу
- `usePersonGroupsAddPerson` - Добавить персону в группу
- `usePersonGroupsRemovePerson` - Удалить персону из группы

### Лица (Faces)
- `useFacesGetFacesPage` - Получить страницу лиц
- `useFacesUpdate` - Обновить лицо

### Пользователи (Users) - Админка
- `useUsersGetAll` - Список пользователей
- `useUsersCreate` - Создать пользователя
- `useUsersUpdate` - Обновить пользователя
- `useUsersDelete` - Удалить пользователя
- `useUsersResetPassword` - Сброс пароля
- `useUsersSetRoles` - Установить роли

### Профили доступа (AdminAccessProfiles)
- `useAdminAccessProfilesList` - Список профилей
- `useAdminAccessProfilesCreate` - Создать профиль
- `useAdminAccessProfilesGet` - Получить профиль
- `useAdminAccessProfilesUpdate` - Обновить профиль
- `useAdminAccessProfilesDelete` - Удалить профиль
- `useAdminAccessProfilesAssignUser` - Назначить пользователя
- `useAdminAccessProfilesUnassignUser` - Отозвать пользователя
- И другие...

### Остальные
- `useStorages` - Хранилища
- `useTags` - Теги
- `useVersion` - Версия API
- `usePaths` - Пути

## TypeScript типы

Все типы доступны для импорта:

```typescript
import type {
  UserDto,
  PhotoItemDto,
  PhotoDto,
  PersonDto,
  PersonGroupDto,
  FaceDto,
  FilterDto,
  LoginRequestDto,
  LoginResponseDto,
  AccessProfileDto,
  ProblemDetails,
  // ... и многие другие
} from '@api';
```

## Обработка ошибок

Все ошибки возвращают тип `ProblemDetails`:

```typescript
import type { ProblemDetails } from '@api';

mutation.onError((error: ProblemDetails) => {
  Alert.alert(
    error.title || 'Error',
    error.detail || 'Something went wrong'
  );
});
```

## Управление токеном

Хелперы для работы с токеном:

```typescript
import { authHelpers } from '@api';

// Сохранить токен
await authHelpers.saveToken('your-jwt-token');

// Получить токен
const token = await authHelpers.getToken();

// Удалить токен (логаут)
await authHelpers.removeToken();
```

Токен автоматически добавляется ко всем запросам через interceptor в `client.ts`.

## Дополнительная информация

Смотрите `src/api/README.md` для более подробных примеров использования, включая:
- Optimistic updates
- Query invalidation
- Advanced patterns
- Error handling

## Troubleshooting

### Ошибка "Cannot find module '@api'"

Убедитесь что:
1. Путь в `tsconfig.json` содержит `"@api/*": ["src/api/*"]`
2. Путь в `babel.config.js` содержит `'@api': './src/api'`
3. Перезапустите Metro bundler

### Ошибка "Module not found: Can't resolve 'axios'"

Запустите:
```bash
pnpm install
```

### Нужно обновить API после изменения OpenAPI

Запустите:
```bash
pnpm run generate:api
```
