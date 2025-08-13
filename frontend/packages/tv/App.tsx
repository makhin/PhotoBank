import React from 'react';

import { AppNavigator } from './utils/navigation';
import { useAutoLogin } from './hooks/useAutoLogin';

export default function App() {
  useAutoLogin();
  return <AppNavigator />;
}
