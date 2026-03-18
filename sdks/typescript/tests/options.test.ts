import { describe, expect, it } from "vitest";
import { validateOptions } from "../src/options.js";

describe("validateOptions", () => {
  it("throws on empty apiKey", () => {
    expect(() => validateOptions({ apiKey: "" })).toThrow("apiKey is required");
  });

  it("throws on whitespace apiKey", () => {
    expect(() => validateOptions({ apiKey: "   " })).toThrow("apiKey is required");
  });

  it("throws on invalid baseUrl", () => {
    expect(() => validateOptions({ apiKey: "cl_test", baseUrl: "not-a-url" })).toThrow(
      "valid absolute URL",
    );
  });

  it("throws on HTTP baseUrl without allowInsecureHttp", () => {
    expect(() => validateOptions({ apiKey: "cl_test", baseUrl: "http://localhost:5000" })).toThrow(
      "HTTPS",
    );
  });

  it("allows HTTP baseUrl with allowInsecureHttp", () => {
    expect(() =>
      validateOptions({
        apiKey: "cl_test",
        baseUrl: "http://localhost:5000",
        allowInsecureHttp: true,
      }),
    ).not.toThrow();
  });

  it("allows valid HTTPS baseUrl", () => {
    expect(() =>
      validateOptions({
        apiKey: "cl_test",
        baseUrl: "https://app.clarive.com",
      }),
    ).not.toThrow();
  });

  it("allows default baseUrl with apiKey", () => {
    expect(() => validateOptions({ apiKey: "cl_test" })).not.toThrow();
  });
});
