namespace RouterManager.Application.Interfaces;

using RouterManager.Domain.Entities;

public interface IProvidersService
{
    Task<IEnumerable<Provider>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<(string Username,string Password)>> GetCredentialsAsync(int providerId, string modelIdentifier, CancellationToken ct = default);
}

public interface IProviderRepository
{
    Task<IEnumerable<Provider>> GetAllAsync(CancellationToken ct = default);
}

public interface ICredentialLookupRepository
{
    Task<IEnumerable<(string Username,string PasswordPlain)>> GetByProviderAndModelAsync(int providerId, string modelIdentifier, CancellationToken ct = default);
}