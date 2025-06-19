import {Navigate, Route, Routes} from "react-router-dom";
import {HomePage} from "@/pages/home/HomePage.tsx";
import {ProfilePage} from "@/pages/profile/ProfilePage.tsx";
import {ThemeProvider} from "@/app/providers/ThemeProvider.tsx";
import {useDispatch, useSelector} from "react-redux";
import type {AppDispatch, RootState} from "@/app/store.ts";
import {useEffect} from "react";
import {loadMetadata} from "@/features/meta/model/metaSlice.ts";

export default function App() {
    const dispatch = useDispatch<AppDispatch>()
    const loaded = useSelector((s: RootState) => s.metadata.loaded)

    useEffect(() => {
        if (!loaded) {
            dispatch(loadMetadata())
        }
    }, [loaded, dispatch])

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
