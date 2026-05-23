using PublicationQualitySystem.Entities;

namespace PublicationQualitySystem.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(string id);
    Task<User?> FindByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByIdAsync(string id);
    Task<List<User>> FindByRolesIdAsync(long roleId);
    Task<List<User>> FindAllAsync(int skip, int take);
}
