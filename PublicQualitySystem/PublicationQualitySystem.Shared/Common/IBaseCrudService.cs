namespace PublicationQualitySystem.Shared.Common;

public interface IBaseCrudService<TCreateRequest, TUpdateRequest, TResponse, Id>
{
    Task<TResponse> CreateAsync(TCreateRequest request);
    Task<TResponse> GetByIdAsync(Id id);
    Task<TResponse> UpdateAsync(Id id,TUpdateRequest request);
    Task DeleteAsync(Id id);
}

