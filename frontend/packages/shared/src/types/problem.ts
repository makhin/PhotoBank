export interface ProblemDetails {
  title: string;
  status: number;
  detail: string;
  type?: string;
  instance?: string;
  [key: string]: unknown;
}

export class ProblemDetailsError extends Error {
  problem: ProblemDetails;
  constructor(problem: ProblemDetails) {
    super(problem.title);
    this.problem = problem;
  }
}

export const isProblemDetails = (value: unknown): value is ProblemDetails => {
  return (
    typeof value === 'object' &&
    value !== null &&
    'title' in value &&
    'status' in value &&
    'detail' in value
  );
};
