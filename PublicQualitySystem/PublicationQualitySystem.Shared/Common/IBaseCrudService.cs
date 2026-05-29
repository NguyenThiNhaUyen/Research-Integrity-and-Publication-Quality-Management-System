namespace PublicationQualitySystem.Shared.Common;

public interface IBaseCrudService<TResponseDto, in TCreateRequestDto, in TUpdateRequestDto, in TId>
{
    Task<TResponseDto> CreateAsync(TCreateRequestDto dto);
    Task<TResponseDto> GetByIdAsync(TId id);
    Task<TResponseDto> UpdateAsync(TId id, TUpdateRequestDto dto);
    Task DeleteAsync(TId id);
    Task<List<TResponseDto>> GetAllAsync(int page, int size);
}

public interface IBaseCrudService<TResponseDto, in TCreateRequestDto, in TUpdateRequestDto>
    : IBaseCrudService<TResponseDto, TCreateRequestDto, TUpdateRequestDto, long>;
