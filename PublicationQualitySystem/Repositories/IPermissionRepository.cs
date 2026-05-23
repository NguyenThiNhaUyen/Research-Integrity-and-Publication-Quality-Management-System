using PublicationQualitySystem.Entities;

namespace PublicationQualitySystem.Repositories;

public interface IPermissionRepository
{
    Task<Permission?> FindByNameAsync(string name);
    Task<List<Permission>> FindAllByNamesAsync(IEnumerable<string> names);
}
