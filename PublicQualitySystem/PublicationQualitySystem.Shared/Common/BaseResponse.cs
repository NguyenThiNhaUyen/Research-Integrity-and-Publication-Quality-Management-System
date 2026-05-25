using System.Text.Json.Serialization;

namespace PublicationQualitySystem.Shared.Common;

public class BaseResponse<T>
{
    public bool Success { get; set; }
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    public static BaseResponse<T> Created(T data) => new()
    {
        Success = true,
        Code = 201,
        Message = "Create Success",
        Data = data
    };

    public static BaseResponse<T> SuccessResponse(T? data, string message) => new()
    {
        Success = true,
        Code = 200,
        Message = message,
        Data = data
    };

    public static BaseResponse<T> Error(int code, string message) => new()
    {
        Success = false,
        Code = code,
        Message = message
    };
}
