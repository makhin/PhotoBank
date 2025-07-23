import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import botReducer from '../src/features/bot/model/botSlice';
import ServiceInfoPage from '../src/pages/service/ServiceInfoPage';
import { serviceInfoTitle } from '@photobank/shared/constants';

describe('ServiceInfoPage', () => {
  it('renders technical information title', () => {
    const store = configureStore({ reducer: { bot: botReducer } });
    render(
      <Provider store={store}>
        <ServiceInfoPage />
      </Provider>
    );
    expect(screen.getByText(serviceInfoTitle)).toBeTruthy();
  });
});
