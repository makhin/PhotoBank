import { ProblemDetailsError, isProblemDetails } from '@photobank/shared/types/problem';

export async function unwrapOrThrow<T>(promise: Promise<{ data: T }>): Promise<T> {
  try {
    const res = await promise;
    return res.data;
  } catch (err: unknown) {
  const maybeProblem: unknown =
    (err as { problem?: unknown })?.problem ??
    (err as { response?: { data?: unknown } })?.response?.data ??
    err;
    if (isProblemDetails(maybeProblem)) {
      throw new ProblemDetailsError(maybeProblem);
    }
    throw err;
  }
}
