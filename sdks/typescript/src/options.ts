export const DEFAULT_BASE_URL = "https://app.clarive.com";

import type { ResilienceOptions } from "./resilience.js";

export interface ClariveOptions {
  apiKey: string;
  baseUrl?: string;
  allowInsecureHttp?: boolean;
  resilience?: ResilienceOptions;
}

export function validateOptions(options: ClariveOptions): void {
  if (!options.apiKey || !options.apiKey.trim()) {
    throw new Error("apiKey is required.");
  }

  const baseUrl = options.baseUrl ?? DEFAULT_BASE_URL;

  let parsed: URL;
  try {
    parsed = new URL(baseUrl);
  } catch {
    throw new Error("baseUrl must be a valid absolute URL.");
  }

  if (!options.allowInsecureHttp && parsed.protocol !== "https:") {
    throw new Error("baseUrl must use HTTPS. Set allowInsecureHttp: true for development.");
  }
}
