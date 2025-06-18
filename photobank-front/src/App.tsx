import {Navigate, Route, Routes} from "react-router-dom";
import {HomePage} from "@/features/home/HomePage";
import {ProfilePage} from "@/features/profile/ProfilePage";
import {ThemeProvider} from "@/components/theme-provider.tsx";

export default function App() {
    return (
        <ThemeProvider defaultTheme="system" storageKey="vite-ui-theme">
            <Routes>
                <Route path="/" element={<HomePage/>}/>
                <Route path="/profile/:userId" element={<ProfilePage/>}/>
                <Route path="*" element={<Navigate to="/" replace/>}/>
            </Routes>
        </ThemeProvider>
    );
}
