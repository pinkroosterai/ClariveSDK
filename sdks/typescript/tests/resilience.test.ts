import { beforeEach, describe, expect, it, vi } from "vitest";
import { CircuitBreaker } from "../src/circuitbreaker.js";
import { ClariveClient } from "../src/client.js";
import { ClariveApiError, ClariveCircuitOpenError, ClariveNotFoundError } from "../src/errors.js";
import type { PromptEntry } from "../src/models.js";

const ENTRY_ID = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

const ENTRY_RESPONSE: PromptEntry = {
  id: ENTRY_ID,
  title: "Test",
  systemMessage: null,
  version: 1,
  prompts: [],
};

function mockResponse(body: unknown, status = 200): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 200 ? "OK" : "Error",
    json: () => Promise.resolve(body),
    headers: new Headers(),
    redirected: false,
    type: "basic",
    url: "",
    clone: () => mockResponse(body, status),
    body: null,
    bodyUsed: false,
    arrayBuffer: () => Promise.resolve(new ArrayBuffer(0)),
    blob: () => Promise.resolve(new Blob()),
    formData: () => Promise.resolve(new FormData()),
    text: () => Promise.resolve(JSON.stringify(body)),
    bytes: () => Promise.resolve(new Uint8Array()),
  } as Response;
}

describe("CircuitBreaker", () => {
  it("starts closed", () => {
    const cb = new CircuitBreaker(3, 10);
    expect(cb.currentState).toBe("closed");
    expect(() => cb.check()).not.toThrow();
  });

  it("opens after threshold failures", () => {
    const cb = new CircuitBreaker(3, 10);
    cb.recordFailure();
    cb.recordFailure();
    expect(cb.currentState).toBe("closed");
    cb.recordFailure();
    expect(cb.currentState).toBe("open");
    expect(() => cb.check()).toThrow(ClariveCircuitOpenError);
  });

  it("success resets failure count", () => {
    const cb = new CircuitBreaker(3, 10);
    cb.recordFailure();
    cb.recordFailure();
    cb.recordSuccess();
    cb.recordFailure();
    cb.recordFailure();
    expect(cb.currentState).toBe("closed");
  });

  it("transitions to half-open after cooldown", () => {
    const cb = new CircuitBreaker(2, 0.001); // Very short cooldown
    cb.recordFailure();
    cb.recordFailure();
    expect(cb.currentState).toBe("open");

    // Wait for cooldown
    return new Promise<void>((resolve) => {
      setTimeout(() => {
        cb.check(); // Should transition to half_open
        expect(cb.currentState).toBe("half_open");
        resolve();
      }, 10);
    });
  });

  it("reset restores to closed", () => {
    const cb = new CircuitBreaker(2, 60);
    cb.recordFailure();
    cb.recordFailure();
    expect(cb.currentState).toBe("open");
    cb.reset();
    expect(cb.currentState).toBe("closed");
  });
});

describe("Retry integration", () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock;
  });

  it("retries on 500 then succeeds", async () => {
    const error500 = mockResponse({ error: { code: "INTERNAL_ERROR", message: "fail" } }, 500);
    const success = mockResponse(ENTRY_RESPONSE);

    fetchMock.mockResolvedValueOnce(error500).mockResolvedValueOnce(success);

    const client = new ClariveClient({
      apiKey: "cl_test",
      resilience: { maxRetries: 3, baseDelay: 0.001 },
    });
    const entry = await client.getEntry(ENTRY_ID);

    expect(entry.title).toBe("Test");
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it("does NOT retry on 404", async () => {
    const error404 = mockResponse({ error: { code: "NOT_FOUND", message: "gone" } }, 404);
    fetchMock.mockResolvedValue(error404);

    const client = new ClariveClient({
      apiKey: "cl_test",
      resilience: { maxRetries: 3, baseDelay: 0.001 },
    });

    await expect(client.getEntry(ENTRY_ID)).rejects.toBeInstanceOf(ClariveNotFoundError);
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("does NOT retry when resilience is disabled", async () => {
    const error500 = mockResponse({ error: { code: "INTERNAL_ERROR", message: "fail" } }, 500);
    fetchMock.mockResolvedValue(error500);

    const client = new ClariveClient({
      apiKey: "cl_test",
      resilience: { enabled: false },
    });

    await expect(client.getEntry(ENTRY_ID)).rejects.toBeInstanceOf(ClariveApiError);
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });
});

describe("Circuit breaker integration", () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock;
  });

  it("opens circuit after repeated failures", async () => {
    const error500 = mockResponse({ error: { code: "INTERNAL_ERROR", message: "fail" } }, 500);
    fetchMock.mockResolvedValue(error500);

    const client = new ClariveClient({
      apiKey: "cl_test",
      resilience: {
        maxRetries: 1,
        baseDelay: 0.001,
        circuitBreakerThreshold: 2,
        circuitBreakerCooldown: 60,
      },
    });

    // First two calls fail and trip the circuit
    await expect(client.getEntry(ENTRY_ID)).rejects.toBeInstanceOf(ClariveApiError);
    await expect(client.getEntry(ENTRY_ID)).rejects.toBeInstanceOf(ClariveApiError);

    // Third call blocked by circuit breaker
    await expect(client.getEntry(ENTRY_ID)).rejects.toBeInstanceOf(ClariveCircuitOpenError);
  });
});
