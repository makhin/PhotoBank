import {
  delay,
  HttpResponse,
  type HttpResponseInit,
  type JsonBodyType,
} from 'msw';

export const withDelay = (ms = 300) => delay(ms);

export const respond = <T>(data: T, init?: number | HttpResponseInit) => {
  const responseInit: HttpResponseInit | undefined =
    typeof init === 'number' ? { status: init } : init;
  return HttpResponse.json(data as JsonBodyType, responseInit);
};

export const respondError = (status = 500, message = 'Server error', extra?: Record<string, unknown>) =>
  HttpResponse.json({ title: message, status, ...extra }, { status });
