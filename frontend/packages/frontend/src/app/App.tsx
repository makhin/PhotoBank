import { useEffect } from 'react';
import { getAuthToken } from '@photobank/shared/auth';

import { ThemeProvider } from '@/app/providers/ThemeProvider';
import NavBar from '@/components/NavBar';
import { useAppDispatch, useAppSelector } from '@/app/hook';
import { loadMetadata } from '@/features/meta/model/metaSlice';
import { AppRoutes } from '@/routes/AppRoutes';
import Lightbox from '@/features/viewer/Lightbox';

export default function App() {
  const dispatch = useAppDispatch();
  const loaded = useAppSelector((s) => s.metadata.loaded);
  const loggedIn = Boolean(getAuthToken());

  useEffect(() => {
    if (loggedIn && !loaded) {
      dispatch(loadMetadata());
    }
  }, [loaded, loggedIn, dispatch]);

  return (
    <ThemeProvider defaultTheme="system" storageKey="vite-ui-theme">
      {loggedIn && <NavBar />}
      <AppRoutes />
      <Lightbox />
    </ThemeProvider>
  );
}
