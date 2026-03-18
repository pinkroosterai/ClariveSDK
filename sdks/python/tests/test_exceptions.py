import pytest

from clarive.exceptions import (
    ClariveApiError,
    ClariveAuthenticationError,
    ClariveCircuitOpenError,
    ClariveError,
    ClariveNotFoundError,
    ClariveRateLimitError,
    ClariveValidationError,
)


class TestClariveApiError:
    def test_str_includes_code_and_message(self) -> None:
        err = ClariveApiError("TEST_CODE", "Something went wrong", 500)
        assert str(err) == "TEST_CODE: Something went wrong"

    def test_attributes(self) -> None:
        err = ClariveApiError("TEST_CODE", "msg", 500)
        assert err.error_code == "TEST_CODE"
        assert err.status_code == 500

    def test_is_exception(self) -> None:
        err = ClariveApiError("X", "y", 500)
        assert isinstance(err, Exception)


class TestFromResponse:
    def test_unauthorized_returns_authentication_error(self) -> None:
        err = ClariveApiError.from_response(401, "UNAUTHORIZED", "Invalid key")
        assert isinstance(err, ClariveAuthenticationError)
        assert err.error_code == "UNAUTHORIZED"
        assert err.status_code == 401

    def test_not_found_returns_not_found_error(self) -> None:
        err = ClariveApiError.from_response(404, "NOT_FOUND", "Entry not found")
        assert isinstance(err, ClariveNotFoundError)
        assert err.error_code == "NOT_FOUND"
        assert err.status_code == 404

    def test_validation_error_returns_validation_error_with_details(self) -> None:
        details = {"age": "Must be an integer", "color": "Must be one of: red, green"}
        err = ClariveApiError.from_response(422, "VALIDATION_ERROR", "Validation failed", details)
        assert isinstance(err, ClariveValidationError)
        assert err.error_code == "VALIDATION_ERROR"
        assert err.status_code == 422
        assert err.details == details

    def test_validation_error_defaults_to_empty_details(self) -> None:
        err = ClariveApiError.from_response(422, "VALIDATION_ERROR", "Failed")
        assert isinstance(err, ClariveValidationError)
        assert err.details == {}

    def test_rate_limited_returns_rate_limit_error(self) -> None:
        err = ClariveApiError.from_response(429, "RATE_LIMITED", "Too many requests")
        assert isinstance(err, ClariveRateLimitError)
        assert err.error_code == "RATE_LIMITED"
        assert err.status_code == 429

    def test_unknown_code_returns_base_error(self) -> None:
        err = ClariveApiError.from_response(500, "INTERNAL_ERROR", "Server error")
        assert type(err) is ClariveApiError
        assert err.error_code == "INTERNAL_ERROR"
        assert err.status_code == 500


class TestExceptionHierarchy:
    def test_api_subtypes_are_clarive_api_error(self) -> None:
        assert issubclass(ClariveAuthenticationError, ClariveApiError)
        assert issubclass(ClariveNotFoundError, ClariveApiError)
        assert issubclass(ClariveValidationError, ClariveApiError)
        assert issubclass(ClariveRateLimitError, ClariveApiError)

    def test_all_errors_are_clarive_error(self) -> None:
        assert issubclass(ClariveApiError, ClariveError)
        assert issubclass(ClariveCircuitOpenError, ClariveError)

    def test_circuit_open_is_not_api_error(self) -> None:
        """ClariveCircuitOpenError should NOT be caught by except ClariveApiError."""
        assert not issubclass(ClariveCircuitOpenError, ClariveApiError)

    def test_catch_api_error_does_not_catch_circuit_open(self) -> None:
        with pytest.raises(ClariveCircuitOpenError):
            try:
                raise ClariveCircuitOpenError()
            except ClariveApiError:
                pytest.fail("ClariveApiError should NOT catch ClariveCircuitOpenError")

    def test_catch_clarive_error_catches_both(self) -> None:
        try:
            raise ClariveNotFoundError("gone")
        except ClariveError:
            pass  # Should catch API errors

        try:
            raise ClariveCircuitOpenError()
        except ClariveError:
            pass  # Should catch resilience errors too

    def test_catch_api_error_catches_subtypes(self) -> None:
        try:
            raise ClariveNotFoundError("gone")
        except ClariveApiError as e:
            assert e.status_code == 404
