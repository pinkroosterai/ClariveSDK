import { describe, expect, it } from "vitest";
import {
  ClariveApiError,
  ClariveAuthenticationError,
  ClariveCircuitOpenError,
  ClariveError,
  ClariveNotFoundError,
  ClariveRateLimitError,
  ClariveValidationError,
} from "../src/errors.js";

describe("ClariveApiError", () => {
  it("has errorCode, statusCode, and message", () => {
    const err = new ClariveApiError("TEST", "Something broke", 500);
    expect(err.errorCode).toBe("TEST");
    expect(err.statusCode).toBe(500);
    expect(err.message).toBe("Something broke");
    expect(err.name).toBe("ClariveApiError");
  });

  it("is instanceof Error and ClariveError", () => {
    const err = new ClariveApiError("X", "y", 500);
    expect(err).toBeInstanceOf(Error);
    expect(err).toBeInstanceOf(ClariveError);
  });
});

describe("fromResponse", () => {
  it("returns ClariveAuthenticationError for UNAUTHORIZED", () => {
    const err = ClariveApiError.fromResponse(401, "UNAUTHORIZED", "Bad key");
    expect(err).toBeInstanceOf(ClariveAuthenticationError);
    expect(err.errorCode).toBe("UNAUTHORIZED");
    expect(err.statusCode).toBe(401);
  });

  it("returns ClariveNotFoundError for NOT_FOUND", () => {
    const err = ClariveApiError.fromResponse(404, "NOT_FOUND", "Gone");
    expect(err).toBeInstanceOf(ClariveNotFoundError);
    expect(err.statusCode).toBe(404);
  });

  it("returns ClariveValidationError with details for VALIDATION_ERROR", () => {
    const details = { age: "Must be integer" };
    const err = ClariveApiError.fromResponse(422, "VALIDATION_ERROR", "Failed", details);
    expect(err).toBeInstanceOf(ClariveValidationError);
    expect((err as ClariveValidationError).details).toEqual(details);
  });

  it("returns ClariveRateLimitError for RATE_LIMITED", () => {
    const err = ClariveApiError.fromResponse(429, "RATE_LIMITED", "Too fast");
    expect(err).toBeInstanceOf(ClariveRateLimitError);
    expect(err.statusCode).toBe(429);
  });

  it("returns base ClariveApiError for unknown codes", () => {
    const err = ClariveApiError.fromResponse(500, "INTERNAL_ERROR", "Oops");
    expect(err.constructor).toBe(ClariveApiError);
    expect(err.errorCode).toBe("INTERNAL_ERROR");
  });
});

describe("exception hierarchy", () => {
  it("all API subtypes are ClariveApiError and ClariveError", () => {
    expect(new ClariveAuthenticationError("x")).toBeInstanceOf(ClariveApiError);
    expect(new ClariveNotFoundError("x")).toBeInstanceOf(ClariveError);
    expect(new ClariveValidationError("x", {})).toBeInstanceOf(ClariveApiError);
    expect(new ClariveRateLimitError("x")).toBeInstanceOf(ClariveApiError);
  });

  it("ClariveCircuitOpenError is ClariveError but NOT ClariveApiError", () => {
    const err = new ClariveCircuitOpenError();
    expect(err).toBeInstanceOf(ClariveError);
    expect(err).not.toBeInstanceOf(ClariveApiError);
    expect(err.name).toBe("ClariveCircuitOpenError");
  });
});
