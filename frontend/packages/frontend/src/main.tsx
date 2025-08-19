import ReactDOM from 'react-dom/client';
import { Provider } from 'react-redux';
import { BrowserRouter } from 'react-router-dom';

import { QueryProvider } from '@/app/providers/QueryProvider';
import { API_BASE_URL } from '@/config';
import { store } from '@/app/store';
import { configureApi } from '@/shared/lib/api';
import { I18nProvider } from '@/app/providers/I18nProvider';

import App from './app/App';

import './index.css';

async function start() {
  configureApi(API_BASE_URL);

  // ВАЖНО: импортируй только по флагу, чтобы не тянуть msw в прод-бандл
  if (import.meta.env.DEV && import.meta.env.VITE_USE_MOCKS === '1') {
    const { worker } = await import('./mocks/browser');
    await worker.start({
      onUnhandledRequest: 'bypass', // реальные вызовы не ломаем
    });
  }

  const root = document.getElementById('root')!;
  ReactDOM.createRoot(root).render(
    <QueryProvider>
      <Provider store={store}>
        <BrowserRouter>
          <I18nProvider>
            <App />
          </I18nProvider>
        </BrowserRouter>
      </Provider>
    </QueryProvider>,
  );
}

start();
