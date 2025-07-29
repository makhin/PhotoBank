import ReactDOM from 'react-dom/client';
import { Provider } from 'react-redux';
import { BrowserRouter } from 'react-router-dom';
import { API_BASE_URL } from '@/config.ts';
import { configureApi } from './lib/api';

import { store } from '@/app/store';

import App from './app/App.tsx';

import './index.css';

function start() {
  configureApi(API_BASE_URL);

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
