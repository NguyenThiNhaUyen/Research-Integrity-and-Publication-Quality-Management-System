using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Repositories.Interfaces;

public interface IPaperRepository
{
    Task<Paper?> FindByIdWithAccessDataAsync(long id);
}
