using System.Net.Http.Json;
using System.Text.Json;
using ClariveSDK.Exceptions;
using ClariveSDK.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClariveSDK;

public class ClariveClient : IClariveClient
{
    private readonly HttpClient _httpClient;
    private readonly ClariveOptions _options;

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [ActivatorUtilitiesConstructor]
    public ClariveClient(HttpClient httpClient, IOptions<ClariveOptions> options)
        : this(httpClient, options?.Value ?? throw new ArgumentNullException(nameof(options))) { }

    public ClariveClient(HttpClient httpClient, ClariveOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        _httpClient = httpClient;
        _options = options;

        if (_httpClient.BaseAddress is null)
        {
            var baseUrl = _options.BaseUrl.TrimEnd('/');
            _httpClient.BaseAddress = new Uri($"{baseUrl}/public/v1/");
        }

        if (!_httpClient.DefaultRequestHeaders.Contains("X-Api-Key"))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _options.ApiKey);
        }
    }

    public async Task<PromptEntry> GetEntryAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"entries/{entryId}", cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var entry = await response.Content.ReadFromJsonAsync<PromptEntry>(JsonOptions, cancellationToken).ConfigureAwait(false);
        return entry ?? throw new InvalidOperationException("Response deserialized to null.");
    }

    public async Task<GenerateResponse> GenerateAsync(Guid entryId, GenerateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await _httpClient.PostAsJsonAsync($"entries/{entryId}/generate", request, JsonOptions, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var result = await response.Content.ReadFromJsonAsync<GenerateResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("Response deserialized to null.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var statusCode = (int)response.StatusCode;

        try
        {
            var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
            if (errorResponse?.Error is { } error)
            {
                throw ClariveApiException.FromApiError(statusCode, error.Code, error.Message, error.Details);
            }
        }
        catch (ClariveApiException)
        {
            throw;
        }
        catch (JsonException)
        {
            // Body wasn't valid JSON or didn't match the error shape
        }

        throw new ClariveApiException("UNKNOWN", response.ReasonPhrase ?? "Unknown error", statusCode);
    }
}
