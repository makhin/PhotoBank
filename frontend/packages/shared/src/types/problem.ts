export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  [k: string]: unknown;
}

export class ProblemDetailsError extends Error {
  constructor(public problem: ProblemDetails) {
    super(problem.title);
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
  return typeof value === 'object' && value !== null && 'title' in value && 'status' in value;
};
