from __future__ import annotations


class ClariveError(Exception):
    """Base exception for all Clarive SDK errors."""


class ClariveApiError(ClariveError):
    """Base exception for errors returned by the Clarive API."""

    def __init__(self, error_code: str, message: str, status_code: int) -> None:
        super().__init__(message)
        self.error_code = error_code
        self.status_code = status_code

    def __str__(self) -> str:
        return f"{self.error_code}: {super().__str__()}"

    @classmethod
    def from_response(
        cls,
        status_code: int,
        code: str,
        message: str,
        details: dict[str, str] | None = None,
    ) -> ClariveApiError:
        """Create the appropriate typed exception for the given API error."""
        match code:
            case "UNAUTHORIZED":
                return ClariveAuthenticationError(message)
            case "NOT_FOUND" | "ENTRY_NOT_FOUND" | "NO_PUBLISHED_VERSION":
                return ClariveNotFoundError(message)
            case "VALIDATION_ERROR":
                return ClariveValidationError(message, details or {})
            case "RATE_LIMITED":
                return ClariveRateLimitError(message)
            case _:
                return cls(code, message, status_code)


class ClariveAuthenticationError(ClariveApiError):
    """Raised when the API key is invalid or missing (401)."""

    def __init__(self, message: str) -> None:
        super().__init__("UNAUTHORIZED", message, 401)


class ClariveNotFoundError(ClariveApiError):
    """Raised when the requested entry does not exist (404)."""

    def __init__(self, message: str) -> None:
        super().__init__("NOT_FOUND", message, 404)


class ClariveValidationError(ClariveApiError):
    """Raised when template field validation fails (422)."""

    def __init__(self, message: str, details: dict[str, str]) -> None:
        super().__init__("VALIDATION_ERROR", message, 422)
        self.details = details


class ClariveRateLimitError(ClariveApiError):
    """Raised when the rate limit is exceeded (429)."""

    def __init__(self, message: str) -> None:
        super().__init__("RATE_LIMITED", message, 429)


class ClariveCircuitOpenError(ClariveError):
    """Raised when the circuit breaker is open and blocking requests."""

    def __init__(self) -> None:
        super().__init__("Circuit breaker is open")
