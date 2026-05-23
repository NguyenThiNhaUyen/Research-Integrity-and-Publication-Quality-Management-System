namespace PublicationQualitySystem.Common;

public interface IBaseCrudService<TDto, in TId>
{
    Task<TDto> CreateAsync(TDto dto);
    Task<TDto> GetByIdAsync(TId id);
    Task<TDto> UpdateAsync(TId id, TDto dto);
    Task DeleteAsync(TId id);
    Task<List<TDto>> GetAllAsync(int page, int size);
}

public interface IBaseCrudService<TDto> : IBaseCrudService<TDto, long>;
