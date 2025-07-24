import ReactDOM from 'react-dom/client';
import { Provider } from 'react-redux';
import { BrowserRouter } from 'react-router-dom';
import { loadResources, getApiBaseUrl } from '@photobank/shared/config';
import { setApiBaseUrl } from '@photobank/shared/api';

import { store } from '@/app/store';

import App from './app/App.tsx';

import './index.css';

async function start() {
  await loadResources();
  setApiBaseUrl(getApiBaseUrl());

  // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
  ReactDOM.createRoot(document.getElementById('root')!).render(
    <Provider store={store}>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </Provider>,
  );
}

start();
