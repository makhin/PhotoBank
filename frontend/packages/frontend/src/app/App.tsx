import { useDispatch, useSelector } from 'react-redux';
import { useEffect } from 'react';
import { getAuthToken } from '@photobank/shared/auth';

import { ThemeProvider } from '@/app/providers/ThemeProvider.tsx';
import NavBar from '@/components/NavBar.tsx';
import type { AppDispatch, RootState } from '@/app/store.ts';
import { loadMetadata } from '@/features/meta/model/metaSlice.ts';
import { AppRoutes } from '@/routes/AppRoutes.tsx';

export default function App() {
  const dispatch = useDispatch<AppDispatch>();
  const loaded = useSelector((s: RootState) => s.metadata.loaded);
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
    </ThemeProvider>
  );
}
