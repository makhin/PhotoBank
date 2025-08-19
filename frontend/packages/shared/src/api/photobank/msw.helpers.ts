import { HttpResponse, delay } from 'msw';

export const respond = <T>(data: T, init?: ResponseInit) =>
  HttpResponse.json(data, init);

export const respondError = (status: number, message: string) =>
  HttpResponse.json({ message }, { status });

export const withDelay = (ms: number) => delay(ms);

