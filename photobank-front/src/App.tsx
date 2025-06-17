import { Routes, Route, Navigate } from "react-router-dom";
import { HomePage } from "@/features/home/HomePage";
import { ProfilePage } from "@/features/profile/ProfilePage";

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/profile/:userId" element={<ProfilePage />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
