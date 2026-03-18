export class ClariveError extends Error {
  constructor(message: string) {
    super(message);
    this.name = this.constructor.name;
  }
}

export class ClariveApiError extends ClariveError {
  readonly errorCode: string;
  readonly statusCode: number;

  constructor(errorCode: string, message: string, statusCode: number) {
    super(message);
    this.errorCode = errorCode;
    this.statusCode = statusCode;
  }

  static fromResponse(
    statusCode: number,
    code: string,
    message: string,
    details?: Record<string, string>,
  ): ClariveApiError {
    switch (code) {
      case "UNAUTHORIZED":
        return new ClariveAuthenticationError(message);
      case "NOT_FOUND":
      case "ENTRY_NOT_FOUND":
      case "NO_PUBLISHED_VERSION":
        return new ClariveNotFoundError(message);
      case "VALIDATION_ERROR":
        return new ClariveValidationError(message, details ?? {});
      case "RATE_LIMITED":
        return new ClariveRateLimitError(message);
      default:
        return new ClariveApiError(code, message, statusCode);
    }
  }
}

export class ClariveAuthenticationError extends ClariveApiError {
  constructor(message: string) {
    super("UNAUTHORIZED", message, 401);
  }
}

export class ClariveNotFoundError extends ClariveApiError {
  constructor(message: string) {
    super("NOT_FOUND", message, 404);
  }
}

export class ClariveValidationError extends ClariveApiError {
  readonly details: Record<string, string>;

  constructor(message: string, details: Record<string, string>) {
    super("VALIDATION_ERROR", message, 422);
    this.details = details;
  }
}

export class ClariveRateLimitError extends ClariveApiError {
  constructor(message: string) {
    super("RATE_LIMITED", message, 429);
  }
}

export class ClariveCircuitOpenError extends ClariveError {
  constructor() {
    super("Circuit breaker is open");
  }
}
