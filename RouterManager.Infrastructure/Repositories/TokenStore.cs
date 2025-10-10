using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RouterManager.Application.Interfaces;
using RouterManager.Infrastructure.Persistence;

namespace RouterManager.Infrastructure.Repositories;

public class TokenStore : ITokenStore
{
    private readonly RouterManagerDbContext _db;
    public TokenStore(RouterManagerDbContext db) => _db = db;

    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        var createTableSql = @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[JwtTokens]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[JwtTokens](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Kind] NVARCHAR(32) NOT NULL, -- 'device' | 'provider' | 'user'
        [Serial] NVARCHAR(256) NULL,
        [ProviderId] INT NULL,
        [UserId] INT NULL,
        [Token] NVARCHAR(MAX) NOT NULL,
        [ExpiresAtUtc] DATETIME2 NULL,
        [CreatedAtUtc] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
";
        await _db.Database.ExecuteSqlRawAsync(createTableSql, ct);

        // Drop antigas constraints/índices se existirem (idempotente)
        var dropConstraintsSql = @"
IF EXISTS (SELECT * FROM sys.objects WHERE name = 'UX_JwtTokens_Device' AND type = 'UQ') ALTER TABLE dbo.JwtTokens DROP CONSTRAINT UX_JwtTokens_Device;
IF EXISTS (SELECT * FROM sys.objects WHERE name = 'UX_JwtTokens_Provider' AND type = 'UQ') ALTER TABLE dbo.JwtTokens DROP CONSTRAINT UX_JwtTokens_Provider;
IF EXISTS (SELECT * FROM sys.objects WHERE name = 'UX_JwtTokens_User' AND type = 'UQ') ALTER TABLE dbo.JwtTokens DROP CONSTRAINT UX_JwtTokens_User;
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_JwtTokens_Device') DROP INDEX UX_JwtTokens_Device ON dbo.JwtTokens;
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_JwtTokens_Provider') DROP INDEX UX_JwtTokens_Provider ON dbo.JwtTokens;
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_JwtTokens_User') DROP INDEX UX_JwtTokens_User ON dbo.JwtTokens;
";
        await _db.Database.ExecuteSqlRawAsync(dropConstraintsSql, ct);

        // Cria índices únicos filtrados por Kind
        var createIndexesSql = @"
CREATE UNIQUE INDEX UX_JwtTokens_Device ON dbo.JwtTokens(Serial) WHERE Kind = 'device';
CREATE UNIQUE INDEX UX_JwtTokens_Provider ON dbo.JwtTokens(ProviderId) WHERE Kind = 'provider';
CREATE UNIQUE INDEX UX_JwtTokens_User ON dbo.JwtTokens(UserId) WHERE Kind = 'user';
";
        await _db.Database.ExecuteSqlRawAsync(createIndexesSql, ct);
    }

    public async Task UpsertDeviceTokenAsync(string serial, string token, DateTimeOffset? expiresAt = null, CancellationToken ct = default)
    {
        await UpsertAsync("device", serial: serial, token: token, expiresAt: expiresAt, ct: ct);
    }

    public async Task UpsertProviderTokenAsync(int providerId, string token, DateTimeOffset? expiresAt = null, CancellationToken ct = default)
    {
        await UpsertAsync("provider", providerId: providerId, token: token, expiresAt: expiresAt, ct: ct);
    }

    public async Task UpsertUserTokenAsync(int userId, string token, DateTimeOffset? expiresAt = null, CancellationToken ct = default)
    {
        await UpsertAsync("user", userId: userId, token: token, expiresAt: expiresAt, ct: ct);
    }

    public async Task<(string Token, DateTimeOffset? ExpiresAtUtc)?> GetDeviceTokenAsync(string serial, CancellationToken ct = default)
    {
        var sql = @"SELECT TOP 1 Token, ExpiresAtUtc FROM dbo.JwtTokens WHERE Kind='device' AND Serial = @Serial ORDER BY Id DESC";
        var p = new[] { new SqlParameter("@Serial", serial) };
        var list = await _db.Database.SqlQueryRaw<TokenRow>(sql, p).ToListAsync(ct);
        var row = list.FirstOrDefault();
        if (row == null) return null;
        DateTimeOffset? exp = row.ExpiresAtUtc.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(row.ExpiresAtUtc.Value, DateTimeKind.Utc))
            : null;
        return (row.Token, exp);
    }

    private record TokenRow(string Token, DateTime? ExpiresAtUtc);

    private async Task UpsertAsync(string kind, string? serial = null, int? providerId = null, int? userId = null, string token = "", DateTimeOffset? expiresAt = null, CancellationToken ct = default)
    {
        var sql = @"
MERGE dbo.JwtTokens AS t
USING (SELECT @Kind AS Kind, @Serial AS Serial, @ProviderId AS ProviderId, @UserId AS UserId) AS s
ON (
    (t.Kind = s.Kind AND s.Kind = 'device' AND t.Serial = s.Serial) OR
    (t.Kind = s.Kind AND s.Kind = 'provider' AND t.ProviderId = s.ProviderId) OR
    (t.Kind = s.Kind AND s.Kind = 'user' AND t.UserId = s.UserId)
)
WHEN MATCHED THEN UPDATE SET Token = @Token, ExpiresAtUtc = @ExpiresAt, CreatedAtUtc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (Kind, Serial, ProviderId, UserId, Token, ExpiresAtUtc) VALUES (@Kind, @Serial, @ProviderId, @UserId, @Token, @ExpiresAt);
";
        var p = new[]
        {
            new SqlParameter("@Kind", kind),
            new SqlParameter("@Serial", (object?)serial ?? DBNull.Value),
            new SqlParameter("@ProviderId", (object?)providerId ?? DBNull.Value),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value),
            new SqlParameter("@Token", token),
            new SqlParameter("@ExpiresAt", (object?)expiresAt?.UtcDateTime ?? DBNull.Value)
        };
        await _db.Database.ExecuteSqlRawAsync(sql, p, ct);
    }
}
