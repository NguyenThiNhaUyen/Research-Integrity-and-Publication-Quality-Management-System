using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Application.Repositories.Interfaces;

namespace PublicationQualitySystem.Infrastructure.Repositories.Implementations;

public class PermissionRepository(ApplicationDbContext db) : IPermissionRepository
{
    public Task<Permission?> FindByNameAsync(string name) =>
        db.Permissions.FirstOrDefaultAsync(p => p.Name == name);

    public Task<List<Permission>> FindAllByNamesAsync(IEnumerable<string> names) =>
        db.Permissions.Where(p => names.Contains(p.Name)).ToListAsync();
}
