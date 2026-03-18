export interface RetryOptions {
  maxRetries: number;
  baseDelay: number;
  shouldRetry: (err: unknown) => boolean;
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

export async function retryWithBackoff<T>(fn: () => Promise<T>, opts: RetryOptions): Promise<T> {
  for (let attempt = 0; attempt <= opts.maxRetries; attempt++) {
    try {
      return await fn();
    } catch (err) {
      if (attempt === opts.maxRetries || !opts.shouldRetry(err)) {
        throw err;
      }

      const delay = opts.baseDelay * 2 ** attempt * (0.5 + Math.random() * 0.5);
      await sleep(delay * 1000);
    }
  }

  // Unreachable — the loop always returns or throws. Required for TypeScript control flow.
  throw new Error("Unreachable");
}
