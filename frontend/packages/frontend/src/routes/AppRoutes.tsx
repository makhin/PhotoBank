import { lazy, Suspense } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';

import RequireAuth from '@/app/RequireAuth';
import RequireAdmin from '@/app/RequireAdmin';
import ErrorBoundary from '@/app/ErrorBoundary';

const PhotoListPage = lazy(() => import('@/pages/list/PhotoListPage'));
const FilterPage = lazy(() => import('@/pages/filter/FilterPage'));
const PhotoDetailsPage = lazy(() => import('@/pages/detail/PhotoDetailsPage'));
const LoginPage = lazy(() => import('@/pages/auth/LoginPage'));
const RegisterPage = lazy(() => import('@/pages/auth/RegisterPage'));
const LogoutPage = lazy(() => import('@/pages/auth/LogoutPage'));
const MyProfilePage = lazy(() => import('@/pages/profile/MyProfilePage'));
const UsersPage = lazy(() => import('@/pages/admin/UsersPage'));
const AccessProfilesPage = lazy(() => import('@/pages/admin/AccessProfilesPage'));
const PersonGroupsPage = lazy(() => import('@/pages/admin/PersonGroupsPage'));
const EditPersonGroupPage = lazy(
  () => import('@/pages/admin/EditPersonGroupPage'),
);
const PersonsPage = lazy(() => import('@/pages/admin/PersonsPage'));
const FacesPage = lazy(() => import('@/pages/admin/FacesPage'));
const ServiceInfoPage = lazy(() => import('@/pages/service/ServiceInfoPage'));
const OpenAIPage = lazy(() => import('@/pages/openai/OpenAIPage'));

export const AppRoutes = () => (
  <Suspense
    fallback={
      <div className="p-6 space-y-4 animate-pulse">
        <div className="h-4 w-1/3 rounded bg-muted" />
        <div className="h-4 w-full rounded bg-muted" />
        <div className="h-4 w-full rounded bg-muted" />
      </div>
    }
  >
    <ErrorBoundary>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/service" element={<ServiceInfoPage />} />
        <Route path="/openai" element={<OpenAIPage />} />
        <Route element={<RequireAuth />}>
          <Route path="/" element={<Navigate to="/filter" />} />
          <Route path="/logout" element={<LogoutPage />} />
          <Route path="/profile" element={<MyProfilePage />} />
          <Route path="/filter" element={<FilterPage />} />
          <Route path="/photos" element={<PhotoListPage />} />
          <Route path="/photos/:id" element={<PhotoDetailsPage />} />
          <Route element={<RequireAdmin />}>
            <Route path="/admin/users" element={<UsersPage />} />
            <Route path="/admin/access-profiles" element={<AccessProfilesPage />} />
            <Route path="/admin/person-groups" element={<PersonGroupsPage />} />
            <Route path="/admin/person-groups/:id" element={<EditPersonGroupPage />} />
            <Route path="/admin/persons" element={<PersonsPage />} />
            <Route path="/admin/faces" element={<FacesPage />} />
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </ErrorBoundary>
  </Suspense>
);

