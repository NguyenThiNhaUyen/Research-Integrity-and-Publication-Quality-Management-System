using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Application.DTOs.User;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IUserService : IBaseCrudService<CreateUserRequest, UpdateUserRequest, UserResponse, string>;
