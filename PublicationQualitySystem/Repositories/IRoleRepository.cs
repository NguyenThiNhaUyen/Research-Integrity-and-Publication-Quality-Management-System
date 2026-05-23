using PublicationQualitySystem.Entities;

namespace PublicationQualitySystem.Repositories;

public interface IRoleRepository
{
    Task<Role?> FindByIdAsync(long id);
    Task<Role?> FindByNameAsync(string name);
    Task<bool> ExistsByNameAsync(string name);
    Task<List<Role>> FindAllAsync(int skip, int take);
    Task<List<Role>> FindAllByIdsAsync(IEnumerable<long> ids);
}
