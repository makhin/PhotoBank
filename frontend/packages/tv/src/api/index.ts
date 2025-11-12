// src/api/index.ts
// Экспорт клиента и хелперов
export { axiosInstance, customInstance, authHelpers } from './client';
export { queryClient } from './queryClient';

// Экспорт всех типов
export * from './generated/photoBankApi.schemas';

// Экспорт хуков и функций по модулям
export * from './generated/auth/auth';
export * from './generated/photos/photos';
export * from './generated/persons/persons';
export * from './generated/person-groups/person-groups';
export * from './generated/faces/faces';
export * from './generated/users/users';
export * from './generated/admin-access-profiles/admin-access-profiles';
export * from './generated/storages/storages';
export * from './generated/tags/tags';
export * from './generated/version/version';
