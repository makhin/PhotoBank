import ReactDOM from 'react-dom/client';
import { Provider } from 'react-redux';
import { BrowserRouter } from 'react-router-dom';
import { QueryProvider } from '@/app/providers/QueryProvider.tsx';

import { API_BASE_URL } from '@/config.ts';
import { store } from '@/app/store';
import { configureApi } from '@/shared/lib/api';

import App from './app/App.tsx';

import './index.css';

function start() {
  configureApi(API_BASE_URL);

   
  ReactDOM.createRoot(document.getElementById('root')!).render(
    <QueryProvider>
      <Provider store={store}>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </Provider>
    </QueryProvider>,
  );
}

start();
