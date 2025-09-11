export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  [k: string]: unknown;
}

export class ProblemDetailsError extends Error {
  public readonly problem: ProblemDetails;

  constructor(problem: ProblemDetails) {
    super(problem.title);
    this.name = 'ProblemDetailsError';
    this.problem = problem;
  }
}

export class HttpError extends Error {
  public readonly status: number;
  public readonly info?: unknown;

  constructor(status: number, info?: unknown) {
    super(`HTTP ${status}`);
    this.name = 'HttpError';
    this.status = status;
    this.info = info;
  }
}

export const isProblemDetails = (value: unknown): value is ProblemDetails => {
  return typeof value === 'object' && value !== null && 'title' in value && 'status' in value;
};
