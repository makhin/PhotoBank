import { namespacedStorage } from '../safeStorage';

const localStore = namespacedStorage('auth', 'local');
const sessionStore = namespacedStorage('auth', 'session');

let token: string | null = null;
let initialized = false;
let onChange: ((t: string | null) => void) | undefined;

function init() {
  if (initialized) return;
  initialized = true;
  token = localStore.get<string>('token') ?? sessionStore.get<string>('token');
}

export function getAuthToken(): string | null {
  init();
  return token;
}

export function setAuthToken(newToken: string | null, persist: boolean): void {
  init();
  token = newToken;
  if (newToken) {
    if (persist) {
      localStore.set('token', newToken);
      sessionStore.remove('token');
    } else {
      sessionStore.set('token', newToken);
      localStore.remove('token');
    }
  } else {
    localStore.remove('token');
    sessionStore.remove('token');
  }
  onChange?.(token);
}

export function configureAuth({ onChange: cb }: { onChange?: (token: string | null) => void } = {}): void {
  onChange = cb;
  init();
}
