using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Api.Common
{
    [ApiController]
    public abstract class BaseCrudController<TCreateRequest, TUpdateRequest, TResponse, Id>
        : ApiBaseController
    {
        protected readonly IBaseCrudService<TCreateRequest, TUpdateRequest, TResponse, Id>? _service;

        protected virtual string? CreatePolicy => null;
        protected virtual string? UpdatePolicy => null;
        protected virtual string? GetByIdPolicy => null;
        protected virtual string? DeletePolicy => null;

        protected BaseCrudController()
        {
        }

        protected BaseCrudController(IBaseCrudService<TCreateRequest, TUpdateRequest, TResponse, Id> service)
        {
            _service = service;
        }

        [HttpPost]
        public virtual async Task<ActionResult<BaseResponse<TResponse>>> CreateAsync([FromBody] TCreateRequest request)
        {
            var authorization = await AuthorizeAsync(CreatePolicy);
            if (authorization is not null) return authorization;

            var result = await CreateEntityAsync(request);
            return CreatedResponse(result);
        }

        [HttpPut("{id}")]
        public virtual async Task<ActionResult<BaseResponse<TResponse>>> UpdateAsync(Id id, [FromBody] TUpdateRequest request)
        {
            var authorization = await AuthorizeAsync(UpdatePolicy);
            if (authorization is not null) return authorization;

            var result = await UpdateEntityAsync(id, request);
            return OkResponse(result, "Update successfully");
        }

        [HttpGet("{id}")]
        public virtual async Task<ActionResult<BaseResponse<TResponse>>> GetByIdAsync(Id id)
        {
            var authorization = await AuthorizeAsync(GetByIdPolicy);
            if (authorization is not null) return authorization;

            var result = await GetEntityByIdAsync(id);
            return OkResponse(result, "Get by id successfully");
        }

        [HttpDelete("{id}")]
        public virtual async Task<ActionResult<BaseResponse<bool>>> DeleteAsync(Id id)
        {
            var authorization = await AuthorizeAsync(DeletePolicy);
            if (authorization is not null) return authorization;

            await DeleteEntityAsync(id);
            return OkResponse(true, "Delete successfully");
        }

        protected virtual Task<TResponse> CreateEntityAsync(TCreateRequest request) =>
            _service?.CreateAsync(request) ?? throw new InvalidOperationException("CRUD service is not configured.");

        protected virtual Task<TResponse> UpdateEntityAsync(Id id, TUpdateRequest request) =>
            _service?.UpdateAsync(id, request) ?? throw new InvalidOperationException("CRUD service is not configured.");

        protected virtual Task<TResponse> GetEntityByIdAsync(Id id) =>
            _service?.GetByIdAsync(id) ?? throw new InvalidOperationException("CRUD service is not configured.");

        protected virtual Task DeleteEntityAsync(Id id) =>
            _service?.DeleteAsync(id) ?? throw new InvalidOperationException("CRUD service is not configured.");

        private async Task<ActionResult?> AuthorizeAsync(string? policy)
        {
            if (policy is null) return null;

            var authorizationService = HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();
            var result = await authorizationService.AuthorizeAsync(User, policy);
            return result.Succeeded ? null : Forbid();
        }
    }
}
