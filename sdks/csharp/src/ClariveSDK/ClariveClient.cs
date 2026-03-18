using System.Net.Http.Json;
using System.Text.Json;
using ClariveSDK.Exceptions;
using ClariveSDK.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClariveSDK;

/// <summary>
/// HTTP client for the Clarive Public API. Provides methods to retrieve and render prompt entries.
/// <para>
/// For dependency injection, use <see cref="ClariveServiceCollectionExtensions.AddClarive(IServiceCollection, Action{ClariveOptions})"/>
/// and inject <see cref="IClariveClient"/>. For standalone usage, construct directly with an <see cref="HttpClient"/>
/// and <see cref="ClariveOptions"/>.
/// </para>
/// </summary>
public class ClariveClient : IClariveClient
{
    private readonly HttpClient _httpClient;
    private readonly ClariveOptions _options;

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance for dependency injection. Prefer this constructor via <c>AddClarive()</c>.
    /// </summary>
    /// <param name="httpClient">The HTTP client provided by <see cref="IHttpClientFactory"/>.</param>
    /// <param name="options">The SDK configuration from the DI container.</param>
    [ActivatorUtilitiesConstructor]
    public ClariveClient(HttpClient httpClient, IOptions<ClariveOptions> options)
        : this(httpClient, options?.Value ?? throw new ArgumentNullException(nameof(options))) { }

    /// <summary>
    /// Initializes a new instance for standalone usage without dependency injection.
    /// </summary>
    /// <param name="httpClient">
    /// The HTTP client to use for requests. The caller owns the lifecycle of this client.
    /// If <see cref="HttpClient.BaseAddress"/> is not set, it will be configured from <paramref name="options"/>.
    /// </param>
    /// <param name="options">The SDK configuration. Must pass validation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> or <paramref name="options"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="options"/> fails validation (missing API key or invalid base URL).</exception>
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

    /// <inheritdoc />
    public async Task<PromptEntry> GetEntryAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"entries/{entryId}", cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var entry = await response.Content.ReadFromJsonAsync<PromptEntry>(JsonOptions, cancellationToken).ConfigureAwait(false);
        return entry ?? throw new InvalidOperationException("Response deserialized to null.");
    }

    /// <inheritdoc />
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
