// Унифицируем возврат ошибки в стиле RTK Query
export function orvalQuery<TArg, TResult>(
  call: (arg: TArg, opt?: { signal?: AbortSignal }) => Promise<TResult>
) {
  return async (arg: TArg, api: { signal: AbortSignal }) => {
    try {
      const data = await call(arg, { signal: api.signal });
      return { data };
    } catch (e: unknown) {
      const err = e as {
        status?: number;
        problem?: unknown;
        message?: unknown;
      };
      return {
        error: {
          status: err.status ?? 500,
          data: err.problem ?? err.message ?? err,
        },
      };
    }
  };
}

export const orvalMutation = orvalQuery; // сигнатура та же
