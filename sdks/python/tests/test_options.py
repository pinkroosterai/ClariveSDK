import pytest

from clarive.options import ClariveOptions


class TestClariveOptionsValidation:
    def test_empty_api_key_raises(self) -> None:
        opts = ClariveOptions(api_key="", base_url="https://app.clarive.com")
        with pytest.raises(ValueError, match="api_key is required"):
            opts.validate()

    def test_whitespace_api_key_raises(self) -> None:
        opts = ClariveOptions(api_key="   ", base_url="https://app.clarive.com")
        with pytest.raises(ValueError, match="api_key is required"):
            opts.validate()

    def test_empty_base_url_raises(self) -> None:
        opts = ClariveOptions(api_key="cl_test", base_url="")
        with pytest.raises(ValueError, match="base_url is required"):
            opts.validate()

    def test_invalid_base_url_raises(self) -> None:
        opts = ClariveOptions(api_key="cl_test", base_url="not-a-url")
        with pytest.raises(ValueError, match="valid absolute URL"):
            opts.validate()

    def test_http_without_allow_insecure_raises(self) -> None:
        opts = ClariveOptions(api_key="cl_test", base_url="http://localhost:5000")
        with pytest.raises(ValueError, match="HTTPS"):
            opts.validate()

    def test_http_with_allow_insecure_passes(self) -> None:
        opts = ClariveOptions(
            api_key="cl_test",
            base_url="http://localhost:5000",
            allow_insecure_http=True,
        )
        opts.validate()  # Should not raise

    def test_valid_https_passes(self) -> None:
        opts = ClariveOptions(api_key="cl_test", base_url="https://app.clarive.com")
        opts.validate()  # Should not raise

    def test_default_base_url_with_api_key_passes(self) -> None:
        opts = ClariveOptions(api_key="cl_test")
        opts.validate()  # Should not raise — default is HTTPS


class TestClariveOptionsRepr:
    def test_api_key_hidden_from_repr(self) -> None:
        opts = ClariveOptions(api_key="cl_secret_key_12345")
        r = repr(opts)
        assert "cl_secret_key_12345" not in r
        assert "api_key" not in r

    def test_repr_shows_other_fields(self) -> None:
        opts = ClariveOptions(api_key="cl_test", base_url="https://example.com")
        r = repr(opts)
        assert "https://example.com" in r
