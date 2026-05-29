using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Application.DTOs.User.Requests;
using PublicationQualitySystem.Application.DTOs.User.Responses;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IUserService : IBaseCrudService<UserResponseDto, CreateUserRequestDto, UpdateUserRequestDto, string>;
