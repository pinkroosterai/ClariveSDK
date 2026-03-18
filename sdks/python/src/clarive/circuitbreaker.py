from __future__ import annotations

import enum
import threading
import time

from clarive.exceptions import ClariveCircuitOpenError


class _State(enum.Enum):
    CLOSED = "closed"
    OPEN = "open"
    HALF_OPEN = "half_open"


class CircuitBreaker:
    """Simple circuit breaker with closed/open/half-open states.

    Thread-safe via threading.Lock (also safe for async since state
    transitions are non-yielding under the GIL).
    """

    def __init__(self, threshold: int = 5, cooldown: float = 30.0) -> None:
        self._threshold = threshold
        self._cooldown = cooldown
        self._failure_count = 0
        self._state = _State.CLOSED
        self._opened_at: float | None = None
        self._lock = threading.Lock()

    @property
    def state(self) -> str:
        return self._state.value

    def check(self) -> None:
        """Check if a request is allowed. Raises ClariveCircuitOpenError if open."""
        with self._lock:
            if self._state == _State.CLOSED:
                return

            if self._state == _State.HALF_OPEN:
                # Allow the probe request through
                return

            # OPEN — check if cooldown has expired
            elapsed = time.monotonic() - (self._opened_at or 0)
            if elapsed >= self._cooldown:
                self._state = _State.HALF_OPEN
                return

            raise ClariveCircuitOpenError()

    def record_success(self) -> None:
        """Record a successful request. Resets the circuit to closed."""
        with self._lock:
            self._failure_count = 0
            self._state = _State.CLOSED
            self._opened_at = None

    def record_failure(self) -> None:
        """Record a failed request. Opens the circuit after threshold failures."""
        with self._lock:
            self._failure_count += 1
            if self._failure_count >= self._threshold:
                self._state = _State.OPEN
                self._opened_at = time.monotonic()

    def reset(self) -> None:
        """Reset the circuit breaker to its initial closed state."""
        with self._lock:
            self._failure_count = 0
            self._state = _State.CLOSED
            self._opened_at = None
