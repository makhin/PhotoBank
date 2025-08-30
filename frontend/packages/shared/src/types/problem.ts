import type { ProblemDetails } from '../api/photobank/model/problemDetails';
export type { ProblemDetails } from '../api/photobank/model/problemDetails';

export class ProblemDetailsError extends Error {
  constructor(public problem: ProblemDetails) {
    super(
      problem.title ??
        (problem.status != null ? `HTTP ${problem.status}` : 'ProblemDetailsError')
    );
    this.name = 'ProblemDetailsError';
  }
}

export class HttpError extends Error {
  constructor(public status: number, public info?: unknown) {
    super(`HTTP ${status}`);
    this.name = 'HttpError';
  }
}

export const isProblemDetails = (value: unknown): value is ProblemDetails => {
  if (typeof value !== 'object' || value === null) {
    return false;
  }
  const obj = value as Record<string, unknown>;
  return ['type', 'title', 'status', 'detail', 'instance'].some((k) => k in obj);
};
