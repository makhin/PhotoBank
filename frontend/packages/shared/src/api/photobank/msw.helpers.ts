import { delay, HttpResponse } from 'msw';

export const withDelay = (ms = 300) => delay(ms);

export const respond = <T>(data: T, init?: number | ResponseInit) =>
  HttpResponse.json(data as any, init);

export const respondError = (status = 500, message = 'Server error', extra?: Record<string, unknown>) =>
  HttpResponse.json({ title: message, status, ...extra }, { status });
