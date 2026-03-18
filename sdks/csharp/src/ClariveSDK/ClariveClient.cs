using System.Net.Http.Json;
using System.Text.Json;
using ClariveSDK.Models;

namespace ClariveSDK;

public class ClariveClient
{
    private readonly HttpClient _httpClient;
    private readonly ClariveOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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
        var response = await _httpClient.GetAsync($"entries/{entryId}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var entry = await response.Content.ReadFromJsonAsync<PromptEntry>(JsonOptions, cancellationToken);
        return entry ?? throw new InvalidOperationException("Response deserialized to null.");
    }

    public async Task<GenerateResponse> GenerateAsync(Guid entryId, GenerateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await _httpClient.PostAsJsonAsync($"entries/{entryId}/generate", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GenerateResponse>(JsonOptions, cancellationToken);
        return result ?? throw new InvalidOperationException("Response deserialized to null.");
    }
}
