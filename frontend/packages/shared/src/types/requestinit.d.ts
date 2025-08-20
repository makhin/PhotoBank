interface RequestInit {
  signal?: AbortSignal | null | undefined;
  /**
   * Allows consumers to extend the RequestInit object with
   * additional, typed properties without using `any`.
   */
  [key: string]: unknown;
}

interface AbortSignal {
  /**
   * Enables passing an `AbortSignal` where a `RequestInit` is expected
   * without violating the index signature requirements above.
   */
  [key: string]: unknown;
}
