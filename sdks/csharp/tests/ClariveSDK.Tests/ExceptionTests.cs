using System.Net;
using System.Text.Json;
using ClariveSDK.Exceptions;
using ClariveSDK.Models;
using ClariveSDK.Tests.Helpers;

namespace ClariveSDK.Tests;

public class ExceptionTests
{
    private const string TestApiKey = "cl_test_key_1234567890abcdef";
    private static readonly Guid TestEntryId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

    private static ClariveClient CreateClientWithErrorResponse(HttpStatusCode statusCode, string errorJson)
    {
        var handler = new MockHttpMessageHandler(_ =>
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(errorJson, System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler);
        var options = new ClariveOptions { ApiKey = TestApiKey, BaseUrl = "https://test.clarive.app" };
        return new ClariveClient(httpClient, options);
    }

    [Fact]
    public void FromApiError_Returns_ClariveAuthenticationException_For_Unauthorized()
    {
        var ex = ClariveApiException.FromApiError(401, "UNAUTHORIZED", "Invalid API key");

        Assert.IsType<ClariveAuthenticationException>(ex);
        Assert.Equal("UNAUTHORIZED", ex.ErrorCode);
        Assert.Equal(401, ex.HttpStatusCode);
        Assert.Equal("Invalid API key", ex.Message);
    }

    [Fact]
    public void FromApiError_Returns_ClariveNotFoundException_For_NotFound()
    {
        var ex = ClariveApiException.FromApiError(404, "NOT_FOUND", "Entry not found");

        Assert.IsType<ClariveNotFoundException>(ex);
        Assert.Equal("NOT_FOUND", ex.ErrorCode);
        Assert.Equal(404, ex.HttpStatusCode);
    }

    [Fact]
    public void FromApiError_Returns_ClariveNotFoundException_For_TabNotFound()
    {
        var ex = ClariveApiException.FromApiError(404, "TAB_NOT_FOUND", "Tab not found.");

        Assert.IsType<ClariveNotFoundException>(ex);
        Assert.Equal("NOT_FOUND", ex.ErrorCode);
        Assert.Equal(404, ex.HttpStatusCode);
    }

    [Fact]
    public void FromApiError_Returns_ClariveValidationException_With_Details()
    {
        var details = new Dictionary<string, string>
        {
            ["age"] = "Field 'age' must be a valid integer.",
            ["color"] = "Field 'color' must be one of: red, green, blue."
        };

        var ex = ClariveApiException.FromApiError(422, "VALIDATION_ERROR", "Validation failed.", details);

        var validationEx = Assert.IsType<ClariveValidationException>(ex);
        Assert.Equal(422, validationEx.HttpStatusCode);
        Assert.Equal(2, validationEx.Details.Count);
        Assert.Equal("Field 'age' must be a valid integer.", validationEx.Details["age"]);
    }

    [Fact]
    public void FromApiError_Returns_ClariveRateLimitException_For_RateLimited()
    {
        var ex = ClariveApiException.FromApiError(429, "RATE_LIMITED", "Too many requests.");

        Assert.IsType<ClariveRateLimitException>(ex);
        Assert.Equal(429, ex.HttpStatusCode);
    }

    [Fact]
    public void FromApiError_Returns_Base_Exception_For_Unknown_Code()
    {
        var ex = ClariveApiException.FromApiError(500, "INTERNAL_ERROR", "Something went wrong.");

        Assert.IsType<ClariveApiException>(ex);
        Assert.IsNotType<ClariveAuthenticationException>(ex);
        Assert.Equal("INTERNAL_ERROR", ex.ErrorCode);
        Assert.Equal(500, ex.HttpStatusCode);
    }

    [Fact]
    public async Task Client_Throws_ClariveAuthenticationException_On_401()
    {
        var json = """{"error": {"code": "UNAUTHORIZED", "message": "Invalid or missing API key."}}""";
        var client = CreateClientWithErrorResponse(HttpStatusCode.Unauthorized, json);

        var ex = await Assert.ThrowsAsync<ClariveAuthenticationException>(
            () => client.GetEntryAsync(TestEntryId));

        Assert.Equal("UNAUTHORIZED", ex.ErrorCode);
        Assert.Equal(401, ex.HttpStatusCode);
    }

    [Fact]
    public async Task Client_Throws_ClariveNotFoundException_On_404()
    {
        var json = """{"error": {"code": "NOT_FOUND", "message": "Entry not found."}}""";
        var client = CreateClientWithErrorResponse(HttpStatusCode.NotFound, json);

        var ex = await Assert.ThrowsAsync<ClariveNotFoundException>(
            () => client.GetEntryAsync(TestEntryId));

        Assert.Equal("NOT_FOUND", ex.ErrorCode);
    }

    [Fact]
    public async Task Client_Throws_ClariveValidationException_On_422_With_Details()
    {
        var json = """
        {
          "error": {
            "code": "VALIDATION_ERROR",
            "message": "Template field validation failed.",
            "details": {
              "age": "Field 'age' must be a valid integer.",
              "color": "Field 'color' must be one of: red, green, blue."
            }
          }
        }
        """;
        var client = CreateClientWithErrorResponse(HttpStatusCode.UnprocessableEntity, json);

        var request = new GenerateRequest { Fields = new() { ["age"] = "abc" } };
        var ex = await Assert.ThrowsAsync<ClariveValidationException>(
            () => client.GenerateAsync(TestEntryId, request));

        Assert.Equal("VALIDATION_ERROR", ex.ErrorCode);
        Assert.Equal(2, ex.Details.Count);
        Assert.Contains("age", ex.Details.Keys);
    }

    [Fact]
    public async Task Client_Throws_ClariveRateLimitException_On_429()
    {
        var json = """{"error": {"code": "RATE_LIMITED", "message": "Too many requests."}}""";
        var client = CreateClientWithErrorResponse((HttpStatusCode)429, json);

        var ex = await Assert.ThrowsAsync<ClariveRateLimitException>(
            () => client.GetEntryAsync(TestEntryId));

        Assert.Equal(429, ex.HttpStatusCode);
    }

    [Fact]
    public async Task Client_Throws_Base_Exception_On_Unknown_Error_Code()
    {
        var json = """{"error": {"code": "INTERNAL_ERROR", "message": "Something broke."}}""";
        var client = CreateClientWithErrorResponse(HttpStatusCode.InternalServerError, json);

        var ex = await Assert.ThrowsAsync<ClariveApiException>(
            () => client.GetEntryAsync(TestEntryId));

        Assert.Equal("INTERNAL_ERROR", ex.ErrorCode);
        Assert.Equal(500, ex.HttpStatusCode);
    }

    [Fact]
    public async Task Client_Throws_Base_Exception_On_NonJson_Error()
    {
        var handler = new MockHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("Bad Gateway", System.Text.Encoding.UTF8, "text/plain")
            });

        var httpClient = new HttpClient(handler);
        var options = new ClariveOptions { ApiKey = TestApiKey, BaseUrl = "https://test.clarive.app" };
        var client = new ClariveClient(httpClient, options);

        var ex = await Assert.ThrowsAsync<ClariveApiException>(
            () => client.GetEntryAsync(TestEntryId));

        Assert.Equal("UNKNOWN", ex.ErrorCode);
        Assert.Equal(502, ex.HttpStatusCode);
    }
}
