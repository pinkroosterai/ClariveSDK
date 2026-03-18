from unittest.mock import patch
from uuid import UUID

import httpx
import pytest
import respx

from clarive.circuitbreaker import CircuitBreaker
from clarive.client import ClariveClient, ClariveClientSync
from clarive.exceptions import (
    ClariveApiError,
    ClariveCircuitOpenError,
    ClariveNotFoundError,
)
from clarive.options import ClariveOptions, ResilienceOptions

ENTRY_ID = UUID("3fa85f64-5717-4562-b3fc-2c963f66afa6")
BASE_URL = "https://app.clarive.com/public/v1/"

ENTRY_RESPONSE = {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Test Entry",
    "systemMessage": None,
    "version": 1,
    "prompts": [],
}


# --- Circuit Breaker Unit Tests ---


class TestCircuitBreaker:
    def test_starts_closed(self) -> None:
        cb = CircuitBreaker(threshold=3, cooldown=10.0)
        assert cb.state == "closed"
        cb.check()  # Should not raise

    def test_opens_after_threshold_failures(self) -> None:
        cb = CircuitBreaker(threshold=3, cooldown=10.0)
        cb.record_failure()
        cb.record_failure()
        assert cb.state == "closed"
        cb.record_failure()
        assert cb.state == "open"
        with pytest.raises(ClariveCircuitOpenError):
            cb.check()

    def test_success_resets_failure_count(self) -> None:
        cb = CircuitBreaker(threshold=3, cooldown=10.0)
        cb.record_failure()
        cb.record_failure()
        cb.record_success()
        cb.record_failure()
        cb.record_failure()
        assert cb.state == "closed"  # Reset by success, only 2 failures now

    @patch("clarive.circuitbreaker.time.monotonic")
    def test_transitions_to_half_open_after_cooldown(
        self,
        mock_time: patch,  # type: ignore[type-arg]
    ) -> None:
        mock_time.return_value = 100.0  # type: ignore[union-attr]
        cb = CircuitBreaker(threshold=2, cooldown=10.0)
        cb.record_failure()
        cb.record_failure()
        assert cb.state == "open"

        # Before cooldown
        mock_time.return_value = 109.0  # type: ignore[union-attr]
        with pytest.raises(ClariveCircuitOpenError):
            cb.check()

        # After cooldown
        mock_time.return_value = 111.0  # type: ignore[union-attr]
        cb.check()  # Should not raise — transitions to half-open
        assert cb.state == "half_open"

    @patch("clarive.circuitbreaker.time.monotonic")
    def test_half_open_success_resets_to_closed(
        self,
        mock_time: patch,  # type: ignore[type-arg]
    ) -> None:
        mock_time.return_value = 100.0  # type: ignore[union-attr]
        cb = CircuitBreaker(threshold=2, cooldown=5.0)
        cb.record_failure()
        cb.record_failure()

        mock_time.return_value = 106.0  # type: ignore[union-attr]
        cb.check()  # Transitions to half-open
        cb.record_success()
        assert cb.state == "closed"

    @patch("clarive.circuitbreaker.time.monotonic")
    def test_half_open_failure_reopens(
        self,
        mock_time: patch,  # type: ignore[type-arg]
    ) -> None:
        mock_time.return_value = 100.0  # type: ignore[union-attr]
        cb = CircuitBreaker(threshold=2, cooldown=5.0)
        cb.record_failure()
        cb.record_failure()

        mock_time.return_value = 106.0  # type: ignore[union-attr]
        cb.check()  # Transitions to half-open
        cb.record_failure()
        cb.record_failure()
        assert cb.state == "open"

    def test_reset(self) -> None:
        cb = CircuitBreaker(threshold=2, cooldown=5.0)
        cb.record_failure()
        cb.record_failure()
        assert cb.state == "open"
        cb.reset()
        assert cb.state == "closed"


# --- Async Client Retry Tests ---


class TestAsyncRetry:
    @respx.mock
    async def test_retries_on_500(self) -> None:
        route = respx.get(f"{BASE_URL}entries/{ENTRY_ID}")
        route.side_effect = [
            httpx.Response(500, json={"error": {"code": "INTERNAL_ERROR", "message": "fail"}}),
            httpx.Response(200, json=ENTRY_RESPONSE),
        ]

        opts = ClariveOptions(
            api_key="cl_test",
            resilience=ResilienceOptions(max_retries=3, base_delay=0.01),
        )
        async with ClariveClient(options=opts) as client:
            entry = await client.get_entry(ENTRY_ID)

        assert entry.title == "Test Entry"
        assert route.call_count == 2

    @respx.mock
    async def test_no_retry_on_404(self) -> None:
        route = respx.get(f"{BASE_URL}entries/{ENTRY_ID}").mock(
            return_value=httpx.Response(
                404, json={"error": {"code": "NOT_FOUND", "message": "gone"}}
            )
        )

        opts = ClariveOptions(
            api_key="cl_test",
            resilience=ResilienceOptions(max_retries=3, base_delay=0.01),
        )
        async with ClariveClient(options=opts) as client:
            with pytest.raises(ClariveNotFoundError):
                await client.get_entry(ENTRY_ID)

        assert route.call_count == 1  # No retry

    @respx.mock
    async def test_resilience_disabled_no_retry(self) -> None:
        route = respx.get(f"{BASE_URL}entries/{ENTRY_ID}").mock(
            return_value=httpx.Response(
                500, json={"error": {"code": "INTERNAL_ERROR", "message": "fail"}}
            )
        )

        opts = ClariveOptions(
            api_key="cl_test",
            resilience=ResilienceOptions(enabled=False),
        )
        async with ClariveClient(options=opts) as client:
            with pytest.raises(ClariveApiError):
                await client.get_entry(ENTRY_ID)

        assert route.call_count == 1  # No retry when disabled


# --- Sync Client Retry Tests ---


class TestSyncRetry:
    def test_retries_on_500(self) -> None:
        call_count = 0

        def handler(request: httpx.Request) -> httpx.Response:
            nonlocal call_count
            call_count += 1
            if call_count == 1:
                return httpx.Response(
                    500, json={"error": {"code": "INTERNAL_ERROR", "message": "fail"}}
                )
            return httpx.Response(200, json=ENTRY_RESPONSE)

        opts = ClariveOptions(
            api_key="cl_test",
            resilience=ResilienceOptions(max_retries=3, base_delay=0.01),
        )
        with ClariveClientSync(options=opts) as client:
            client._client = httpx.Client(
                base_url=str(client._client.base_url),
                headers=dict(client._client.headers),
                transport=httpx.MockTransport(handler),
            )
            entry = client.get_entry(ENTRY_ID)

        assert entry.title == "Test Entry"
        assert call_count == 2

    def test_no_retry_on_404(self) -> None:
        call_count = 0

        def handler(request: httpx.Request) -> httpx.Response:
            nonlocal call_count
            call_count += 1
            return httpx.Response(404, json={"error": {"code": "NOT_FOUND", "message": "gone"}})

        opts = ClariveOptions(
            api_key="cl_test",
            resilience=ResilienceOptions(max_retries=3, base_delay=0.01),
        )
        with ClariveClientSync(options=opts) as client:
            client._client = httpx.Client(
                base_url=str(client._client.base_url),
                headers=dict(client._client.headers),
                transport=httpx.MockTransport(handler),
            )
            with pytest.raises(ClariveNotFoundError):
                client.get_entry(ENTRY_ID)

        assert call_count == 1


# --- Circuit Breaker Integration ---


class TestAsyncCircuitBreakerIntegration:
    @respx.mock
    async def test_circuit_opens_after_failures(self) -> None:
        respx.get(f"{BASE_URL}entries/{ENTRY_ID}").mock(
            return_value=httpx.Response(
                500, json={"error": {"code": "INTERNAL_ERROR", "message": "fail"}}
            )
        )

        opts = ClariveOptions(
            api_key="cl_test",
            resilience=ResilienceOptions(
                max_retries=1,
                base_delay=0.01,
                circuit_breaker_threshold=2,
                circuit_breaker_cooldown=60.0,
            ),
        )
        async with ClariveClient(options=opts) as client:
            # First two calls fail and trip the circuit breaker
            with pytest.raises(ClariveApiError):
                await client.get_entry(ENTRY_ID)
            with pytest.raises(ClariveApiError):
                await client.get_entry(ENTRY_ID)

            # Third call should be blocked by circuit breaker
            with pytest.raises(ClariveCircuitOpenError):
                await client.get_entry(ENTRY_ID)
