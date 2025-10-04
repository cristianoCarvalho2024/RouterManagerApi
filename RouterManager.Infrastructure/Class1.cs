using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using RouterManager.Domain.Entities;

namespace RouterManager.Infrastructure.Persistence;

public class RouterManagerDbContext : DbContext
{
    private readonly IDataProtector _protector;

    public RouterManagerDbContext(DbContextOptions<RouterManagerDbContext> options, IDataProtectionProvider provider)
        : base(options)
    {
        _protector = provider.CreateProtector("RouterCredentialProtector");
    }

    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<RouterModel> RouterModels => Set<RouterModel>();
    public DbSet<RouterCredential> RouterCredentials => Set<RouterCredential>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<TelemetryLog> TelemetryLogs => Set<TelemetryLog>();
    public DbSet<UpdatePackage> UpdatePackages => Set<UpdatePackage>();
    public DbSet<UpdateAction> UpdateActions => Set<UpdateAction>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RouterProfile> RouterProfiles => Set<RouterProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Device>().HasIndex(x => x.SerialNumber).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

        // Alterado para 1:N: RouterModel -> RouterCredentials
        modelBuilder.Entity<RouterModel>()
            .HasMany(r => r.Credentials)
            .WithOne(c => c.RouterModel)
            .HasForeignKey(c => c.RouterModelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RouterProfile>().HasIndex(rp => rp.SerialNumber).IsUnique();
    }

    public string Protect(string plain) => _protector.Protect(plain);
    public string Unprotect(string encrypted) => _protector.Unprotect(encrypted);
}
