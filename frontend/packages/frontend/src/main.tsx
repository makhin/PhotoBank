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
