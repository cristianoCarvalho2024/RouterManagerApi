using Microsoft.EntityFrameworkCore;
using RouterManager.Application.Interfaces;
using RouterManager.Domain.Entities;
using RouterManager.Infrastructure.Persistence;

namespace RouterManager.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly RouterManagerDbContext _ctx;
    public UserRepository(RouterManagerDbContext ctx) => _ctx = ctx;

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => _ctx.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        _ctx.Users.Add(user);
        await _ctx.SaveChangesAsync(ct);
    }
}