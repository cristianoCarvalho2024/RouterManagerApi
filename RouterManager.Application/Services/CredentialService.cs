using RouterManager.Application.Interfaces;
using RouterManager.Shared.Dtos.Responses;

namespace RouterManager.Application.Services;

public class CredentialService : ICredentialService
{
    private readonly ICredentialRepository _repo;
    public CredentialService(ICredentialRepository repo) => _repo = repo;

    public async Task<CredentialsResponse?> GetCredentialsAsync(int providerId, string modelIdentifier, CancellationToken ct = default)
    {
        var result = await _repo.GetPlainByProviderAndModelAsync(providerId, modelIdentifier, ct);
        if (result == null) return null;
        var response = new CredentialsResponse
        {
            ProviderId = providerId,
            Model = modelIdentifier,
            Credentials = new List<CredentialItem> { new(result.Value.Username, result.Value.PasswordPlain) }
        };
        return response;
    }
}