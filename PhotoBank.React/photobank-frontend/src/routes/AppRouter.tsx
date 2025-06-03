// src/routes/AppRouter.tsx
import { Routes, Route } from 'react-router-dom';
import PhotoListPage from '../pages/PhotoListPage';
import PhotoDetailPage from '../pages/PhotoDetailPage';

export function AppRouter() {
    return (
        <Routes>
            <Route path="/" element={<PhotoListPage />} />
            <Route path="/photo/:id" element={<PhotoDetailPage />} />
        </Routes>
    );
}
