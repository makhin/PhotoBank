import {ThemeProvider} from "@/app/providers/ThemeProvider.tsx";
import {useDispatch, useSelector} from "react-redux";
import type {AppDispatch, RootState} from "@/app/store.ts";
import {useEffect} from "react";
import {loadMetadata} from "@/features/meta/model/metaSlice.ts";
import {AppRoutes} from "@/routes/AppRoutes.tsx";

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
            <AppRoutes/>
        </ThemeProvider>
    );
}
