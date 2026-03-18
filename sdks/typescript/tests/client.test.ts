import { beforeEach, describe, expect, it, vi } from "vitest";
import { ClariveClient } from "../src/client.js";
import { ClariveApiError, ClariveNotFoundError, ClariveValidationError } from "../src/errors.js";
import type { GenerateResponse, PromptEntry } from "../src/models.js";

const ENTRY_ID = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
const BASE_URL = "https://app.clarive.com/public/v1";

const ENTRY_RESPONSE: PromptEntry = {
  id: ENTRY_ID,
  title: "Test Entry",
  systemMessage: null,
  version: 1,
  prompts: [
    {
      content: "Hello {{name}}",
      order: 1,
      isTemplate: true,
      templateFields: [
        {
          id: "00000000-0000-0000-0000-000000000001",
          promptId: "00000000-0000-0000-0000-000000000002",
          name: "name",
          type: "string",
          enumValues: null,
          defaultValue: null,
          min: null,
          max: null,
        },
      ],
    },
  ],
};

const GENERATE_RESPONSE: GenerateResponse = {
  id: ENTRY_ID,
  title: "Test Entry",
  version: 1,
  systemMessage: null,
  renderedPrompts: [{ content: "Hello Alice", order: 1 }],
};

function mockFetchResponse(body: unknown, status = 200): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 200 ? "OK" : "Error",
    json: () => Promise.resolve(body),
    headers: new Headers(),
    redirected: false,
    type: "basic",
    url: "",
    clone: () => mockFetchResponse(body, status),
    body: null,
    bodyUsed: false,
    arrayBuffer: () => Promise.resolve(new ArrayBuffer(0)),
    blob: () => Promise.resolve(new Blob()),
    formData: () => Promise.resolve(new FormData()),
    text: () => Promise.resolve(JSON.stringify(body)),
    bytes: () => Promise.resolve(new Uint8Array()),
  } as Response;
}

describe("ClariveClient", () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    globalThis.fetch = fetchMock;
  });

  describe("getEntry", () => {
    it("sends GET with X-Api-Key header and returns PromptEntry", async () => {
      fetchMock.mockResolvedValue(mockFetchResponse(ENTRY_RESPONSE));

      const client = new ClariveClient({ apiKey: "cl_testkey" });
      const entry = await client.getEntry(ENTRY_ID);

      expect(fetchMock).toHaveBeenCalledOnce();
      const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
      expect(url).toBe(`${BASE_URL}/entries/${ENTRY_ID}`);
      expect(init.method).toBe("GET");
      expect((init.headers as Record<string, string>)["X-Api-Key"]).toBe("cl_testkey");

      expect(entry.id).toBe(ENTRY_ID);
      expect(entry.title).toBe("Test Entry");
      expect(entry.prompts).toHaveLength(1);
    });

    it("throws ClariveNotFoundError on 404 with JSON body", async () => {
      const errorBody = { error: { code: "NOT_FOUND", message: "Entry not found" } };
      fetchMock.mockResolvedValue(mockFetchResponse(errorBody, 404));

      const client = new ClariveClient({ apiKey: "cl_testkey", resilience: { enabled: false } });
      await expect(client.getEntry(ENTRY_ID)).rejects.toBeInstanceOf(ClariveNotFoundError);
    });

    it("throws ClariveApiError with UNKNOWN on non-JSON error body", async () => {
      const badResponse = {
        ...mockFetchResponse({}, 500),
        ok: false,
        status: 500,
        statusText: "Internal Server Error",
        json: () => Promise.reject(new SyntaxError("Unexpected token")),
      } as Response;
      fetchMock.mockResolvedValue(badResponse);

      const client = new ClariveClient({
        apiKey: "cl_testkey",
        resilience: { enabled: false },
      });
      try {
        await client.getEntry(ENTRY_ID);
        expect.unreachable("Should have thrown");
      } catch (e) {
        expect(e).toBeInstanceOf(ClariveApiError);
        expect((e as ClariveApiError).errorCode).toBe("UNKNOWN");
        expect((e as ClariveApiError).statusCode).toBe(500);
      }
    });
  });

  describe("generate", () => {
    it("sends POST with JSON body and returns GenerateResponse", async () => {
      fetchMock.mockResolvedValue(mockFetchResponse(GENERATE_RESPONSE));

      const client = new ClariveClient({ apiKey: "cl_testkey" });
      const result = await client.generate(ENTRY_ID, {
        fields: { name: "Alice" },
      });

      expect(fetchMock).toHaveBeenCalledOnce();
      const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
      expect(url).toBe(`${BASE_URL}/entries/${ENTRY_ID}/generate`);
      expect(init.method).toBe("POST");
      expect((init.headers as Record<string, string>)["Content-Type"]).toBe("application/json");
      expect(init.body).toBe(JSON.stringify({ fields: { name: "Alice" } }));

      expect(result.renderedPrompts[0].content).toBe("Hello Alice");
    });

    it("throws ClariveValidationError on 422 with details", async () => {
      const errorBody = {
        error: { code: "VALIDATION_ERROR", message: "Failed", details: { name: "Required" } },
      };
      fetchMock.mockResolvedValue(mockFetchResponse(errorBody, 422));

      const client = new ClariveClient({ apiKey: "cl_testkey" });
      try {
        await client.generate(ENTRY_ID, { fields: {} });
        expect.unreachable("Should have thrown");
      } catch (e) {
        expect(e).toBeInstanceOf(ClariveValidationError);
        expect((e as ClariveValidationError).details).toEqual({ name: "Required" });
      }
    });
  });

  describe("constructor", () => {
    it("throws on invalid options", () => {
      expect(() => new ClariveClient({ apiKey: "" })).toThrow("apiKey is required");
    });

    it("hides apiKey from JSON.stringify", () => {
      const client = new ClariveClient({ apiKey: "cl_secret_key_12345" });
      const json = JSON.stringify(client);
      expect(json).not.toContain("cl_secret_key_12345");
      expect(json).not.toContain("apiKey");
      expect(json).toContain("baseUrl");
    });

    it("uses custom baseUrl", async () => {
      fetchMock.mockResolvedValue(mockFetchResponse(ENTRY_RESPONSE));

      const client = new ClariveClient({
        apiKey: "cl_testkey",
        baseUrl: "https://custom.clarive.app",
      });
      await client.getEntry(ENTRY_ID);

      const [url] = fetchMock.mock.calls[0] as [string];
      expect(url).toContain("custom.clarive.app");
    });
  });
});
