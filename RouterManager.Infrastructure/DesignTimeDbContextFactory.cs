using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.DataProtection;
using RouterManager.Infrastructure.Persistence;
using System.IO;

namespace RouterManager.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<RouterManagerDbContext>
{
    public RouterManagerDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("../RouterManager.Api/appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<RouterManagerDbContext>();
        var cs = configuration.GetConnectionString("Default") ?? "Server=.;Database=RouterManagerDb;Trusted_Connection=True;TrustServerCertificate=True";
        optionsBuilder.UseSqlServer(cs);
        var provider = new NoOpDataProtectionProvider();
        return new RouterManagerDbContext(optionsBuilder.Options, provider);
    }
}

internal class NoOpDataProtectionProvider : IDataProtectionProvider
{
    public IDataProtector CreateProtector(string purpose) => new NoOpProtector();
    private class NoOpProtector : IDataProtector
    {
        public IDataProtector CreateProtector(string purpose) => this;
        public byte[] Protect(byte[] plaintext) => plaintext;
        public byte[] Unprotect(byte[] protectedData) => protectedData;
    }
}