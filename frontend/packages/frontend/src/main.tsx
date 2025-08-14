import ReactDOM from 'react-dom/client';
import { Provider } from 'react-redux';
import { BrowserRouter } from 'react-router-dom';

import { API_BASE_URL } from '@/config.ts';
import { store } from '@/app/store';
import { configureApi } from '@/shared/lib/api';

import App from './app/App.tsx';

import './index.css';

function start() {
  configureApi(API_BASE_URL);

   
  ReactDOM.createRoot(document.getElementById('root')!).render(
    <Provider store={store}>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </Provider>,
  );
}

start();
