import { Navigate, Route, Routes } from 'react-router-dom';
import RequireAuth from '@/app/RequireAuth.tsx';

import PhotoListPage from '@/pages/list/PhotoListPage.tsx';
import FilterPage from '@/pages/filter/FilterPage.tsx';
import PhotoDetailsPage from '@/pages/detail/PhotoDetailsPage.tsx';
import LoginPage from '@/pages/auth/LoginPage.tsx';
import LogoutPage from '@/pages/auth/LogoutPage.tsx';
import MyProfilePage from '@/pages/profile/MyProfilePage.tsx';

export const AppRoutes = () => (
  <Routes>
    <Route path="/login" element={<LoginPage />} />
    <Route element={<RequireAuth />}>
      <Route path="/" element={<Navigate to="/filter" />} />
      <Route path="/logout" element={<LogoutPage />} />
      <Route path="/profile" element={<MyProfilePage />} />
      <Route path="/filter" element={<FilterPage />} />
      <Route path="/photos" element={<PhotoListPage />} />
      <Route path="/photos/:id" element={<PhotoDetailsPage />} />
    </Route>
    <Route path="*" element={<Navigate to="/login" replace />} />
  </Routes>
);
