// Унифицируем возврат ошибки в стиле RTK Query
export function orvalQuery<TArg, TResult>(
  call: (arg: TArg, opt?: { signal?: AbortSignal }) => Promise<TResult>
) {
  return async (arg: TArg, api: { signal: AbortSignal }) => {
    try {
      const data = await call(arg, { signal: api.signal });
      return { data };
    } catch (e: any) {
      return { error: { status: e.status ?? 500, data: e.problem ?? e.message ?? e } };
    }
  };
}

export const orvalMutation = orvalQuery; // сигнатура та же
