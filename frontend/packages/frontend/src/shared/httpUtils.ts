import { ProblemDetailsError, isProblemDetails, type ProblemDetails } from '@photobank/shared/types/problem';

export async function unwrapOrThrow<T>(promise: Promise<{ data: T }>): Promise<T> {
  try {
    const res = await promise;
    return res.data;
  } catch (err: unknown) {
    const maybeProblem: unknown = (err as any)?.problem ?? (err as any)?.response?.data ?? err;
    if (isProblemDetails(maybeProblem)) {
      throw new ProblemDetailsError(maybeProblem as ProblemDetails);
    }
    throw err;
  }
}
