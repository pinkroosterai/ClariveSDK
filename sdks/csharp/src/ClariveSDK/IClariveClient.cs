using ClariveSDK.Models;

namespace ClariveSDK;

public interface IClariveClient
{
    Task<PromptEntry> GetEntryAsync(Guid entryId, CancellationToken cancellationToken = default);

    Task<GenerateResponse> GenerateAsync(Guid entryId, GenerateRequest request, CancellationToken cancellationToken = default);
}
