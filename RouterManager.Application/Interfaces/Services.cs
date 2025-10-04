namespace RouterManager.Application.Interfaces;

using RouterManager.Shared.Dtos.Responses;
using RouterManager.Shared.Dtos.Requests;
using RouterManager.Domain.Entities;

public interface ICredentialService
{
    Task<CredentialsResponse?> GetCredentialsAsync(int providerId, string modelIdentifier, CancellationToken ct = default);
}

public interface ITelemetryService
{
    Task ReportAsync(ReportStatusRequest request, CancellationToken ct = default);
}

public interface IUpdateService
{
    Task<UpdatePackageResponse?> CheckAsync(CheckForUpdateRequest request, CancellationToken ct = default);
}

public interface ICredentialRepository
{
    Task<(string Username, string PasswordPlain)?> GetPlainByProviderAndModelAsync(int providerId, string modelIdentifier, CancellationToken ct = default);
}

public interface IDeviceRepository
{
    Task<Device> GetOrCreateAsync(string serial, int routerModelId, string firmware, CancellationToken ct = default);
    Task UpdateAsync(Device device, CancellationToken ct = default);
}

public interface IRouterModelRepository
{
    Task<RouterModel?> GetAsync(int providerId, string modelIdentifier, CancellationToken ct = default);
}

public interface ITelemetryRepository
{
    Task AddLogAsync(TelemetryLog log, CancellationToken ct = default);
}

public interface IUpdateRepository
{
    Task<UpdatePackage?> GetApplicableAsync(int providerId, string modelIdentifier, string firmwareVersion, CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}