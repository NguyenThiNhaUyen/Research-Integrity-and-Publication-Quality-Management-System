using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Configurations;
using PublicationQualitySystem.Entities;
using PublicationQualitySystem.Repositories.Interfaces;

namespace PublicationQualitySystem.Repositories.Implementations;

public class UserRepository(ApplicationDbContext db) : IUserRepository
{
    public Task<User?> FindByIdAsync(string id) =>
        db.Users.Include(u => u.Roles).ThenInclude(r => r.Permissions).FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> FindByEmailAsync(string email) =>
        db.Users.Include(u => u.Roles).ThenInclude(r => r.Permissions).FirstOrDefaultAsync(u => u.Email == email);

    public Task<bool> ExistsByEmailAsync(string email) => db.Users.AnyAsync(u => u.Email == email);

    public Task<bool> ExistsByIdAsync(string id) => db.Users.AnyAsync(u => u.Id == id);

    public Task<List<User>> FindByRolesIdAsync(long roleId) =>
        db.Users.Include(u => u.Roles).Where(u => u.Roles.Any(r => r.Id == roleId)).ToListAsync();

    public Task<List<User>> FindAllAsync(int skip, int take) =>
        db.Users.Include(u => u.Roles).ThenInclude(r => r.Permissions).Where(u => !u.Deleted).Skip(skip).Take(take).ToListAsync();
}
