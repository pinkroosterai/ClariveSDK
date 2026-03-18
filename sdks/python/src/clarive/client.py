from __future__ import annotations

import json
from types import TracebackType
from typing import Any
from uuid import UUID

import httpx
from tenacity import retry, retry_if_exception, stop_after_attempt, wait_exponential_jitter

from clarive.circuitbreaker import CircuitBreaker
from clarive.exceptions import ClariveApiError, ClariveRateLimitError
from clarive.models import GenerateRequest, GenerateResponse, PromptEntry
from clarive.options import ClariveOptions


def _build_base_url(base_url: str) -> str:
    return f"{base_url.rstrip('/')}/public/v1/"


def _handle_error_response(response: httpx.Response) -> None:
    """Parse API error response and raise the appropriate typed exception."""
    if response.is_success:
        return

    status_code = response.status_code
    try:
        body: dict[str, Any] = response.json()
        error = body["error"]
        raise ClariveApiError.from_response(
            status_code=status_code,
            code=error["code"],
            message=error["message"],
            details=error.get("details"),
        )
    except (json.JSONDecodeError, KeyError, TypeError):
        raise ClariveApiError(
            "UNKNOWN",
            response.reason_phrase or "Unknown error",
            status_code,
        ) from None


def _is_transient(exc: BaseException) -> bool:
    """Return True if the exception represents a transient/retryable failure."""
    if isinstance(exc, httpx.TimeoutException):
        return True
    if isinstance(exc, ClariveRateLimitError):
        return True
    return isinstance(exc, ClariveApiError) and exc.status_code >= 500


class _BaseClariveClient:
    """Shared initialization and resilience logic for async and sync clients."""

    def __init__(
        self,
        api_key: str | None = None,
        base_url: str | None = None,
        *,
        options: ClariveOptions | None = None,
    ) -> None:
        if options is not None:
            self._options = options
        else:
            self._options = ClariveOptions(
                api_key=api_key or "",
                base_url=base_url or "https://app.clarive.com",
            )
        self._options.validate()

        resilience = self._options.resilience
        self._resilience_enabled = resilience.enabled
        self._circuit_breaker = CircuitBreaker(
            threshold=resilience.circuit_breaker_threshold,
            cooldown=resilience.circuit_breaker_cooldown,
        )

        self._base_url = _build_base_url(self._options.base_url)
        self._headers = {"X-Api-Key": self._options.api_key}
        self._timeout = resilience.timeout

        # Cache the retry decorator once at init time (not per request)
        self._retrying: Any = None
        if resilience.enabled:
            self._retrying = retry(
                stop=stop_after_attempt(resilience.max_retries),
                wait=wait_exponential_jitter(initial=resilience.base_delay),
                retry=retry_if_exception(_is_transient),
                reraise=True,
            )


class ClariveClient(_BaseClariveClient):
    """Async client for the Clarive Public API.

    Use as an async context manager::

        async with ClariveClient(api_key="cl_...") as client:
            entry = await client.get_entry(entry_id)

    Or manage the lifecycle manually::

        client = ClariveClient(api_key="cl_...")
        try:
            entry = await client.get_entry(entry_id)
        finally:
            await client.aclose()
    """

    def __init__(
        self,
        api_key: str | None = None,
        base_url: str | None = None,
        *,
        options: ClariveOptions | None = None,
    ) -> None:
        super().__init__(api_key, base_url, options=options)
        self._client = httpx.AsyncClient(
            base_url=self._base_url,
            headers=self._headers,
            timeout=self._timeout,
        )

    async def __aenter__(self) -> ClariveClient:
        return self

    async def __aexit__(
        self,
        exc_type: type[BaseException] | None,
        exc_val: BaseException | None,
        exc_tb: TracebackType | None,
    ) -> None:
        await self.aclose()

    async def aclose(self) -> None:
        """Close the underlying HTTP client."""
        await self._client.aclose()

    async def _do_request(
        self,
        method: str,
        url: str,
        json_body: dict[str, Any] | None = None,
    ) -> httpx.Response:
        """Execute the raw HTTP request and handle error responses."""
        response = await self._client.request(method, url, json=json_body)
        _handle_error_response(response)
        return response

    async def _request_with_resilience(
        self,
        method: str,
        url: str,
        json_body: dict[str, Any] | None = None,
    ) -> httpx.Response:
        """Execute an HTTP request with optional retry and circuit breaker."""
        if not self._resilience_enabled or self._retrying is None:
            return await self._do_request(method, url, json_body)

        self._circuit_breaker.check()

        try:
            response: httpx.Response = await self._retrying(self._do_request)(
                method, url, json_body
            )
            self._circuit_breaker.record_success()
            return response
        except Exception:
            self._circuit_breaker.record_failure()
            raise

    async def get_entry(self, entry_id: UUID) -> PromptEntry:
        """Retrieve the published version of a prompt entry.

        Args:
            entry_id: The unique identifier of the prompt entry.

        Returns:
            The published prompt entry with its prompts and template fields.

        Raises:
            ClariveNotFoundError: The entry does not exist or is trashed.
            ClariveAuthenticationError: The API key is invalid or missing.
            ClariveRateLimitError: The rate limit has been exceeded.
            ClariveCircuitOpenError: The circuit breaker is open.
        """
        response = await self._request_with_resilience("GET", f"entries/{entry_id}")
        data: dict[str, object] = response.json()
        return PromptEntry.from_dict(data)

    async def generate(self, entry_id: UUID, request: GenerateRequest) -> GenerateResponse:
        """Render a prompt entry by substituting template variables.

        Args:
            entry_id: The unique identifier of the prompt entry.
            request: The template field values to substitute.

        Returns:
            The rendered prompts with all template variables replaced.

        Raises:
            ClariveValidationError: Template field values are invalid.
            ClariveNotFoundError: The entry does not exist or is trashed.
            ClariveAuthenticationError: The API key is invalid or missing.
            ClariveRateLimitError: The rate limit has been exceeded.
            ClariveCircuitOpenError: The circuit breaker is open.
        """
        response = await self._request_with_resilience(
            "POST",
            f"entries/{entry_id}/generate",
            json_body=request.to_dict(),
        )
        data: dict[str, object] = response.json()
        return GenerateResponse.from_dict(data)


class ClariveClientSync(_BaseClariveClient):
    """Synchronous client for the Clarive Public API.

    Use as a context manager::

        with ClariveClientSync(api_key="cl_...") as client:
            entry = client.get_entry(entry_id)

    Or manage the lifecycle manually::

        client = ClariveClientSync(api_key="cl_...")
        try:
            entry = client.get_entry(entry_id)
        finally:
            client.close()
    """

    def __init__(
        self,
        api_key: str | None = None,
        base_url: str | None = None,
        *,
        options: ClariveOptions | None = None,
    ) -> None:
        super().__init__(api_key, base_url, options=options)
        self._client = httpx.Client(
            base_url=self._base_url,
            headers=self._headers,
            timeout=self._timeout,
        )

    def __enter__(self) -> ClariveClientSync:
        return self

    def __exit__(
        self,
        exc_type: type[BaseException] | None,
        exc_val: BaseException | None,
        exc_tb: TracebackType | None,
    ) -> None:
        self.close()

    def close(self) -> None:
        """Close the underlying HTTP client."""
        self._client.close()

    def _do_request(
        self,
        method: str,
        url: str,
        json_body: dict[str, Any] | None = None,
    ) -> httpx.Response:
        """Execute the raw HTTP request and handle error responses."""
        response = self._client.request(method, url, json=json_body)
        _handle_error_response(response)
        return response

    def _request_with_resilience(
        self,
        method: str,
        url: str,
        json_body: dict[str, Any] | None = None,
    ) -> httpx.Response:
        """Execute an HTTP request with optional retry and circuit breaker."""
        if not self._resilience_enabled or self._retrying is None:
            return self._do_request(method, url, json_body)

        self._circuit_breaker.check()

        try:
            response: httpx.Response = self._retrying(self._do_request)(method, url, json_body)
            self._circuit_breaker.record_success()
            return response
        except Exception:
            self._circuit_breaker.record_failure()
            raise

    def get_entry(self, entry_id: UUID) -> PromptEntry:
        """Retrieve the published version of a prompt entry.

        Args:
            entry_id: The unique identifier of the prompt entry.

        Returns:
            The published prompt entry with its prompts and template fields.

        Raises:
            ClariveNotFoundError: The entry does not exist or is trashed.
            ClariveAuthenticationError: The API key is invalid or missing.
            ClariveRateLimitError: The rate limit has been exceeded.
            ClariveCircuitOpenError: The circuit breaker is open.
        """
        response = self._request_with_resilience("GET", f"entries/{entry_id}")
        data: dict[str, object] = response.json()
        return PromptEntry.from_dict(data)

    def generate(self, entry_id: UUID, request: GenerateRequest) -> GenerateResponse:
        """Render a prompt entry by substituting template variables.

        Args:
            entry_id: The unique identifier of the prompt entry.
            request: The template field values to substitute.

        Returns:
            The rendered prompts with all template variables replaced.

        Raises:
            ClariveValidationError: Template field values are invalid.
            ClariveNotFoundError: The entry does not exist or is trashed.
            ClariveAuthenticationError: The API key is invalid or missing.
            ClariveRateLimitError: The rate limit has been exceeded.
            ClariveCircuitOpenError: The circuit breaker is open.
        """
        response = self._request_with_resilience(
            "POST",
            f"entries/{entry_id}/generate",
            json_body=request.to_dict(),
        )
        data: dict[str, object] = response.json()
        return GenerateResponse.from_dict(data)
