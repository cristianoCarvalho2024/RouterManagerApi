namespace RouterManager.Application.Interfaces;

public interface ITokenStore
{
    Task EnsureSchemaAsync(CancellationToken ct = default);
    Task UpsertDeviceTokenAsync(string serial, string token, DateTimeOffset? expiresAt = null, CancellationToken ct = default);
    Task UpsertProviderTokenAsync(int providerId, string token, DateTimeOffset? expiresAt = null, CancellationToken ct = default);
    Task UpsertUserTokenAsync(int userId, string token, DateTimeOffset? expiresAt = null, CancellationToken ct = default);
    Task<(string Token, DateTimeOffset? ExpiresAtUtc)?> GetDeviceTokenAsync(string serial, CancellationToken ct = default);
}
