using ClariveSDK.Models;

namespace ClariveSDK;

/// <summary>
/// Defines the contract for a Clarive API client.
/// Inject this interface in your services for testability.
/// </summary>
public interface IClariveClient
{
    /// <summary>
    /// Retrieves the currently published version of a prompt entry.
    /// </summary>
    /// <param name="entryId">The unique identifier of the prompt entry.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The published prompt entry with its prompts and template field definitions.</returns>
    /// <exception cref="Exceptions.ClariveNotFoundException">The entry does not exist or is trashed.</exception>
    /// <exception cref="Exceptions.ClariveAuthenticationException">The API key is invalid or missing.</exception>
    /// <exception cref="Exceptions.ClariveRateLimitException">The rate limit has been exceeded.</exception>
    Task<PromptEntry> GetEntryAsync(Guid entryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a published prompt entry by substituting template variables with the provided values.
    /// </summary>
    /// <param name="entryId">The unique identifier of the prompt entry.</param>
    /// <param name="request">The template field values to substitute into the prompts.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The rendered prompts with all template variables replaced.</returns>
    /// <exception cref="Exceptions.ClariveValidationException">One or more template field values are invalid.</exception>
    /// <exception cref="Exceptions.ClariveNotFoundException">The entry does not exist or is trashed.</exception>
    /// <exception cref="Exceptions.ClariveAuthenticationException">The API key is invalid or missing.</exception>
    /// <exception cref="Exceptions.ClariveRateLimitException">The rate limit has been exceeded.</exception>
    Task<GenerateResponse> GenerateAsync(Guid entryId, GenerateRequest request, CancellationToken cancellationToken = default);
}
