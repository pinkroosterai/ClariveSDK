from __future__ import annotations

from dataclasses import dataclass, field
from urllib.parse import urlparse


@dataclass(slots=True)
class ResilienceOptions:
    """Options for retry, circuit breaker, and timeout behavior."""

    enabled: bool = True
    max_retries: int = 3
    base_delay: float = 1.0
    timeout: float = 30.0
    circuit_breaker_threshold: int = 5
    circuit_breaker_cooldown: float = 30.0


@dataclass(slots=True)
class ClariveOptions:
    """Configuration for the Clarive SDK client."""

    api_key: str = field(default="", repr=False)
    base_url: str = "https://app.clarive.com"
    allow_insecure_http: bool = False
    resilience: ResilienceOptions = field(default_factory=ResilienceOptions)

    def validate(self) -> None:
        """Validate that all required options are set and well-formed.

        Raises:
            ValueError: If api_key is empty, base_url is invalid, or base_url
                uses HTTP without allow_insecure_http enabled.
        """
        if not self.api_key or not self.api_key.strip():
            raise ValueError("api_key is required.")

        if not self.base_url or not self.base_url.strip():
            raise ValueError("base_url is required.")

        parsed = urlparse(self.base_url)
        if not parsed.scheme or not parsed.netloc:
            raise ValueError("base_url must be a valid absolute URL.")

        if not self.allow_insecure_http and parsed.scheme != "https":
            raise ValueError(
                "base_url must use HTTPS. Set allow_insecure_http=True for development."
            )
