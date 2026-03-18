namespace ClariveSDK.Tests;

public class ClariveOptionsTests
{
    [Fact]
    public void Validate_Accepts_Https_Url()
    {
        var options = new ClariveOptions
        {
            ApiKey = "cl_test_key",
            BaseUrl = "https://demo.clarive.app"
        };

        var ex = Record.Exception(() => options.Validate());
        Assert.Null(ex);
    }

    [Fact]
    public void Validate_Rejects_Http_Url()
    {
        var options = new ClariveOptions
        {
            ApiKey = "cl_test_key",
            BaseUrl = "http://demo.clarive.app"
        };

        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("HTTPS", ex.Message);
    }

    [Fact]
    public void Validate_Allows_Http_When_AllowInsecureHttp()
    {
        var options = new ClariveOptions
        {
            ApiKey = "cl_test_key",
            BaseUrl = "http://localhost:5000",
            AllowInsecureHttp = true
        };

        var ex = Record.Exception(() => options.Validate());
        Assert.Null(ex);
    }

    [Fact]
    public void Validate_Rejects_Empty_BaseUrl()
    {
        var options = new ClariveOptions
        {
            ApiKey = "cl_test_key",
            BaseUrl = ""
        };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void Validate_Rejects_Invalid_Uri()
    {
        var options = new ClariveOptions
        {
            ApiKey = "cl_test_key",
            BaseUrl = "not-a-url"
        };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void Validate_Rejects_Empty_ApiKey()
    {
        var options = new ClariveOptions
        {
            ApiKey = "",
            BaseUrl = "https://demo.clarive.app"
        };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }
}
