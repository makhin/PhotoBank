import {Navigate, Route, Routes} from "react-router-dom";
import {PhotoListPage} from "@/pages/list/PhotoListPage.tsx";
import {FilterPage} from "@/pages/filter/FilterPage.tsx";
import {PhotoDetailsPage} from "@/pages/detail/PhotoDetailsPage.tsx";

export const AppRoutes = () => (
    <Routes>
        <Route path="/" element={<Navigate to="/filter"/>}/>
        <Route path="/filter" element={<FilterPage/>}/>
        <Route path="/photos" element={<PhotoListPage/>}/>
        <Route path="/photos/:id" element={<PhotoDetailsPage/>}/>
    </Routes>
);
