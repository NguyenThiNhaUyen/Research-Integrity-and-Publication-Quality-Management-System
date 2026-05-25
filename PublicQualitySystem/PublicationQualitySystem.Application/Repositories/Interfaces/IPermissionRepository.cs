using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Repositories.Interfaces;

public interface IPermissionRepository
{
    Task<Permission?> FindByNameAsync(string name);
    Task<List<Permission>> FindAllByNamesAsync(IEnumerable<string> names);
}
