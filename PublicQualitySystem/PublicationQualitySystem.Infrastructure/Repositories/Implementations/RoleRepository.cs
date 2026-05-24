using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Application.Repositories.Interfaces;

namespace PublicationQualitySystem.Infrastructure.Repositories.Implementations;

public class RoleRepository(ApplicationDbContext db) : IRoleRepository
{
    public Task<Role?> FindByIdAsync(long id) =>
        db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id);

    public Task<Role?> FindByNameAsync(string name) =>
        db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Name == name);

    public Task<bool> ExistsByNameAsync(string name) => db.Roles.AnyAsync(r => r.Name == name);

    public Task<List<Role>> FindAllAsync(int skip, int take) =>
        db.Roles.Include(r => r.Permissions).Skip(skip).Take(take).ToListAsync();

    public Task<List<Role>> FindAllByIdsAsync(IEnumerable<long> ids) =>
        db.Roles.Include(r => r.Permissions).Where(r => ids.Contains(r.Id)).ToListAsync();
}
