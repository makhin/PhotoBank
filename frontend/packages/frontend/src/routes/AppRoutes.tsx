import { Navigate, Route, Routes } from 'react-router-dom';
import RequireAuth from '@/app/RequireAuth.tsx';
import RequireAdmin from '@/app/RequireAdmin.tsx';

import PhotoListPage from '@/pages/list/PhotoListPage.tsx';
import FilterPage from '@/pages/filter/FilterPage.tsx';
import PhotoDetailsPage from '@/pages/detail/PhotoDetailsPage.tsx';
import LoginPage from '@/pages/auth/LoginPage.tsx';
import RegisterPage from '@/pages/auth/RegisterPage.tsx';
import LogoutPage from '@/pages/auth/LogoutPage.tsx';
import MyProfilePage from '@/pages/profile/MyProfilePage.tsx';
import UsersPage from '@/pages/admin/UsersPage.tsx';
import ServiceInfoPage from '@/pages/service/ServiceInfoPage.tsx';
import OpenAIPage from '@/pages/openai/OpenAIPage.tsx';

export const AppRoutes = () => (
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
      </Route>
    </Route>
    <Route path="*" element={<Navigate to="/login" replace />} />
  </Routes>
);
