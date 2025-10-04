using RouterManager.Application.Interfaces;
using RouterManager.Domain.Entities;

namespace RouterManager.Application.Services;

public class ProvidersService : IProvidersService
{
    private readonly IProviderRepository _providers;
    private readonly ICredentialLookupRepository _credentials;
    public ProvidersService(IProviderRepository providers, ICredentialLookupRepository credentials)
    { _providers = providers; _credentials = credentials; }

    public Task<IEnumerable<Provider>> GetAllAsync(CancellationToken ct = default) => _providers.GetAllAsync(ct);

    public async Task<IEnumerable<(string Username, string Password)>> GetCredentialsAsync(int providerId, string modelIdentifier, CancellationToken ct = default)
    {
        var list = await _credentials.GetByProviderAndModelAsync(providerId, modelIdentifier, ct);
        return list.Select(c => (c.Username, c.PasswordPlain));
    }
}