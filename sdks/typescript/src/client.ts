import { CircuitBreaker } from "./circuitbreaker.js";
import { ClariveApiError, ClariveRateLimitError } from "./errors.js";
import type {
  EntrySummary,
  GenerateRequest,
  GenerateResponse,
  ListEntriesOptions,
  PaginatedResponse,
  PromptEntry,
  TabSummary,
  TagInfo,
} from "./models.js";
import { type ClariveOptions, DEFAULT_BASE_URL, validateOptions } from "./options.js";
import { DEFAULT_RESILIENCE_OPTIONS } from "./resilience.js";
import { type RetryOptions, retryWithBackoff } from "./retry.js";

async function handleErrorResponse(response: Response): Promise<void> {
  if (response.ok) return;

  const statusCode = response.status;
  try {
    const body = (await response.json()) as {
      error?: { code?: string; message?: string; details?: Record<string, string> };
    };
    const error = body?.error;
    if (error?.code && error?.message) {
      throw ClariveApiError.fromResponse(statusCode, error.code, error.message, error.details);
    }
  } catch (e) {
    if (e instanceof ClariveApiError) throw e;
  }

  throw new ClariveApiError("UNKNOWN", response.statusText || "Unknown error", statusCode);
}

function isTransient(err: unknown): boolean {
  if (err instanceof DOMException && err.name === "AbortError") return true;
  if (err instanceof ClariveRateLimitError) return true;
  if (err instanceof ClariveApiError && err.statusCode >= 500) return true;
  return false;
}

export class ClariveClient {
  private readonly baseUrl: string;
  private readonly apiKey: string;
  private readonly resilienceEnabled: boolean;
  private readonly timeout: number;
  private readonly circuitBreaker: CircuitBreaker;
  private readonly retryOptions: RetryOptions;

  constructor(options: ClariveOptions) {
    validateOptions(options);
    const raw = (options.baseUrl ?? DEFAULT_BASE_URL).replace(/\/+$/, "");
    this.baseUrl = `${raw}/public/v1`;
    this.apiKey = options.apiKey;

    const r = { ...DEFAULT_RESILIENCE_OPTIONS, ...options.resilience };
    this.resilienceEnabled = r.enabled;
    this.timeout = r.timeout;
    this.circuitBreaker = new CircuitBreaker(r.circuitBreakerThreshold, r.circuitBreakerCooldown);

    // Cache retry options at init time (not rebuilt per request)
    this.retryOptions = {
      maxRetries: r.maxRetries,
      baseDelay: r.baseDelay,
      shouldRetry: isTransient,
    };
  }

  toJSON(): Record<string, unknown> {
    return { baseUrl: this.baseUrl, resilienceEnabled: this.resilienceEnabled };
  }

  private async doRequest(method: string, url: string, body?: string): Promise<Response> {
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), this.timeout * 1000);

    try {
      const headers: Record<string, string> = { "X-Api-Key": this.apiKey };
      if (body) headers["Content-Type"] = "application/json";

      const response = await fetch(url, {
        method,
        headers,
        body,
        signal: controller.signal,
      });

      await handleErrorResponse(response);
      return response;
    } finally {
      clearTimeout(timer);
    }
  }

  private async requestWithResilience(
    method: string,
    url: string,
    body?: string,
  ): Promise<Response> {
    if (!this.resilienceEnabled) {
      return this.doRequest(method, url, body);
    }

    this.circuitBreaker.check();

    try {
      const response = await retryWithBackoff(
        () => this.doRequest(method, url, body),
        this.retryOptions,
      );
      this.circuitBreaker.recordSuccess();
      return response;
    } catch (err) {
      this.circuitBreaker.recordFailure();
      throw err;
    }
  }

  async getEntry(entryId: string): Promise<PromptEntry> {
    const response = await this.requestWithResilience("GET", `${this.baseUrl}/entries/${entryId}`);
    return (await response.json()) as PromptEntry;
  }

  async generate(entryId: string, request: GenerateRequest): Promise<GenerateResponse> {
    const response = await this.requestWithResilience(
      "POST",
      `${this.baseUrl}/entries/${entryId}/generate`,
      JSON.stringify(request),
    );
    return (await response.json()) as GenerateResponse;
  }

  async listEntries(options?: ListEntriesOptions): Promise<PaginatedResponse<EntrySummary>> {
    const params = new URLSearchParams();
    if (options?.folderId) params.set("folderId", options.folderId);
    if (options?.tags) params.set("tags", options.tags);
    if (options?.tagMode) params.set("tagMode", options.tagMode);
    if (options?.page) params.set("page", String(options.page));
    if (options?.pageSize) params.set("pageSize", String(options.pageSize));
    if (options?.search) params.set("search", options.search);
    if (options?.sortBy) params.set("sortBy", options.sortBy);

    const query = params.toString();
    const url = query ? `${this.baseUrl}/entries?${query}` : `${this.baseUrl}/entries`;
    const response = await this.requestWithResilience("GET", url);
    return (await response.json()) as PaginatedResponse<EntrySummary>;
  }

  async listTags(): Promise<TagInfo[]> {
    const response = await this.requestWithResilience("GET", `${this.baseUrl}/tags`);
    return (await response.json()) as TagInfo[];
  }

  async listTabs(entryId: string): Promise<TabSummary[]> {
    const response = await this.requestWithResilience(
      "GET",
      `${this.baseUrl}/entries/${entryId}/tabs`,
    );
    return (await response.json()) as TabSummary[];
  }

  async getTab(entryId: string, tabId: string): Promise<PromptEntry> {
    const response = await this.requestWithResilience(
      "GET",
      `${this.baseUrl}/entries/${entryId}/tabs/${tabId}`,
    );
    return (await response.json()) as PromptEntry;
  }

  async generateTab(
    entryId: string,
    tabId: string,
    request: GenerateRequest,
  ): Promise<GenerateResponse> {
    const response = await this.requestWithResilience(
      "POST",
      `${this.baseUrl}/entries/${entryId}/tabs/${tabId}/generate`,
      JSON.stringify(request),
    );
    return (await response.json()) as GenerateResponse;
  }
}
