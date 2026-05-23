using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Configurations;
using PublicationQualitySystem.Entities;
using PublicationQualitySystem.Repositories.Interfaces;

namespace PublicationQualitySystem.Repositories.Implementations;

public class PermissionRepository(ApplicationDbContext db) : IPermissionRepository
{
    public Task<Permission?> FindByNameAsync(string name) =>
        db.Permissions.FirstOrDefaultAsync(p => p.Name == name);

    public Task<List<Permission>> FindAllByNamesAsync(IEnumerable<string> names) =>
        db.Permissions.Where(p => names.Contains(p.Name)).ToListAsync();
}
