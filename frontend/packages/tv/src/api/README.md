# PhotoBank API Integration

Этот проект использует Orval для автоматической генерации TypeScript типов и React Query хуков из OpenAPI спецификации.

## Структура

```
src/api/
├── client.ts              # Axios клиент с interceptors
├── queryClient.ts         # React Query конфигурация
├── index.ts               # Единая точка экспорта всех типов и хуков
└── generated/             # Автоматически сгенерированный код (не редактировать!)
    ├── photoBankApi.schemas.ts
    ├── auth/
    ├── photos/
    ├── persons/
    └── ...
```

## Генерация API

После изменения `openapi.yaml`, запустите:

```bash
pnpm run generate:api
```

Или в режиме watch:

```bash
pnpm run generate:api:watch
```

## Использование

### Настройка базового URL

Обновите `src/api/client.ts`:

```typescript
export const axiosInstance = axios.create({
  baseURL: 'https://your-api.com/api',
  // ...
});
```

### Примеры использования

#### 1. Логин (Mutation)

```typescript
import { useAuthLogin, authHelpers } from '@/api';

function LoginScreen() {
  const loginMutation = useAuthLogin({
    mutation: {
      onSuccess: async (data) => {
        // Сохраняем токен
        if (data.token) {
          await authHelpers.saveToken(data.token);
          // Навигация на главный экран
        }
      },
      onError: (error) => {
        console.error('Login failed:', error);
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
    <View>
      <Button
        title="Login"
        onPress={handleLogin}
        disabled={loginMutation.isPending}
      />
      {loginMutation.isError && <Text>Error: {loginMutation.error.title}</Text>}
    </View>
  );
}
```

#### 2. Получение данных пользователя (Query)

```typescript
import { useAuthGetUser } from '@/api';

function ProfileScreen() {
  const { data: user, isLoading, error, refetch } = useAuthGetUser();

  if (isLoading) return <LoadingSpinner />;
  if (error) return <ErrorMessage error={error} />;

  return (
    <View>
      <Text>Email: {user?.email}</Text>
      <Text>Phone: {user?.phoneNumber}</Text>
      <Button title="Refresh" onPress={() => refetch()} />
    </View>
  );
}
```

#### 3. Поиск фотографий с фильтрами

```typescript
import { usePhotosSearchPhotos, FilterDto } from '@/api';

function PhotoGalleryScreen() {
  const [filters, setFilters] = useState<FilterDto>({
    page: 1,
    pageSize: 20,
    storages: [1, 2],
    isBW: false,
  });

  const searchMutation = usePhotosSearchPhotos({
    mutation: {
      onSuccess: (data) => {
        console.log(`Found ${data.totalCount} photos`);
      },
    },
  });

  useEffect(() => {
    searchMutation.mutate({ data: filters });
  }, [filters]);

  return (
    <View>
      {searchMutation.isLoading && <LoadingSpinner />}
      {searchMutation.data?.items?.map((photo) => (
        <PhotoCard key={photo.id} photo={photo} />
      ))}
    </View>
  );
}
```

#### 4. Загрузка файлов

```typescript
import { usePhotosUpload } from '@/api';

function UploadScreen() {
  const uploadMutation = usePhotosUpload({
    mutation: {
      onSuccess: () => {
        console.log('Upload successful');
      },
    },
  });

  const handleUpload = async (fileUri: string) => {
    // Подготовка FormData для React Native
    const formData = new FormData();
    formData.append('files', {
      uri: fileUri,
      type: 'image/jpeg',
      name: 'photo.jpg',
    } as unknown as Blob);
    formData.append('storageId', '1');
    formData.append('path', '/uploads/');

    uploadMutation.mutate({ data: formData });
  };

  return <Button title="Upload Photo" onPress={() => handleUpload(photoUri)} />;
}
```

#### 5. Работа с типами

```typescript
import type {
  UserDto,
  PhotoItemDto,
  PersonDto,
  LoginRequestDto,
} from '@/api';

// Используйте типы для props
interface UserCardProps {
  user: UserDto;
}

function UserCard({ user }: UserCardProps) {
  return <Text>{user.email}</Text>;
}

// Используйте типы для state
const [currentUser, setCurrentUser] = useState<UserDto | null>(null);

// Используйте типы для функций
function processPhoto(photo: PhotoItemDto): string {
  return photo.thumbnailUrl || '';
}
```

## Продвинутое использование

### Invalidate Queries после мутации

```typescript
import { useQueryClient } from '@tanstack/react-query';
import { usePersonsCreate, getPersonsGetAllQueryKey } from '@/api';

function CreatePersonScreen() {
  const queryClient = useQueryClient();

  const createMutation = usePersonsCreate({
    mutation: {
      onSuccess: () => {
        // Инвалидируем кеш списка персон
        queryClient.invalidateQueries({
          queryKey: getPersonsGetAllQueryKey(),
        });
      },
    },
  });

  // ...
}
```

### Optimistic Updates

```typescript
const updateMutation = useAuthUpdateUser({
  mutation: {
    onMutate: async (newData) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: getAuthGetUserQueryKey() });

      // Snapshot the previous value
      const previousUser = queryClient.getQueryData(getAuthGetUserQueryKey());

      // Optimistically update
      queryClient.setQueryData(getAuthGetUserQueryKey(), newData);

      return { previousUser };
    },
    onError: (err, newData, context) => {
      // Rollback on error
      queryClient.setQueryData(getAuthGetUserQueryKey(), context?.previousUser);
    },
    onSettled: () => {
      // Refetch после успеха или ошибки
      queryClient.invalidateQueries({ queryKey: getAuthGetUserQueryKey() });
    },
  },
});
```

## Обработка ошибок

Все ошибки API следуют типу `ProblemDetails`:

```typescript
import type { ProblemDetails } from '@/api';

function handleApiError(error: ProblemDetails) {
  console.error(`Error ${error.status}: ${error.title}`);
  console.error(`Detail: ${error.detail}`);

  // Показать пользователю
  Alert.alert(error.title || 'Error', error.detail || 'An error occurred');
}
```

## Environment Variables

Создайте `.env` файл:

```
API_BASE_URL=https://your-api.com/api
```

## Полезные ссылки

- [React Query Docs](https://tanstack.com/query/latest)
- [Orval Docs](https://orval.dev/)
- [Axios Docs](https://axios-http.com/)
