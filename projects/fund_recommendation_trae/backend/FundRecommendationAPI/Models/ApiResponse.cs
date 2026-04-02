using System;
using System.Text.Json.Serialization;

namespace FundRecommendationAPI.Models
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("code")]
        public int Code { get; set; } = 200;

        [JsonPropertyName("message")]
        public string Message { get; set; } = "success";

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }

        public static ApiResponse<T> Success(T data)
        {
            return new ApiResponse<T>
            {
                Code = 200,
                Message = "success",
                Data = data,
                Timestamp = DateTime.UtcNow
            };
        }

        public static ApiResponse<T> Success(T data, string message)
        {
            return new ApiResponse<T>
            {
                Code = 200,
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow
            };
        }

        public static ApiResponse<T> Error(int code, string message)
        {
            return new ApiResponse<T>
            {
                Code = code,
                Message = message,
                Data = default,
                Timestamp = DateTime.UtcNow
            };
        }

        public static ApiResponse<T> Error(int code, string message, string? details = null)
        {
            return new ApiResponse<T>
            {
                Code = code,
                Message = message,
                Data = default,
                Timestamp = DateTime.UtcNow
            };
        }

        public static ApiResponse<T> BadRequest(string message)
        {
            return Error(400, message);
        }

        public static ApiResponse<T> NotFound(string message)
        {
            return Error(404, message);
        }

        public static ApiResponse<T> Unauthorized(string message)
        {
            return Error(401, message);
        }

        public static ApiResponse<T> Forbidden(string message)
        {
            return Error(403, message);
        }

        public static ApiResponse<T> InternalError(string message)
        {
            return Error(500, message);
        }
    }

    public class PaginatedResponse<T>
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("page_size")]
        public int PageSize { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);

        [JsonPropertyName("list")]
        public List<T> List { get; set; } = new();
    }

    public static class ErrorCodes
    {
        public const int Success = 200;
        public const int BadRequest = 400;
        public const int Unauthorized = 401;
        public const int Forbidden = 403;
        public const int NotFound = 404;
        public const int Conflict = 409;
        public const int UnprocessableEntity = 422;
        public const int TooManyRequests = 429;
        public const int InternalServerError = 500;
        public const int BadGateway = 502;
        public const int ServiceUnavailable = 503;
        public const int GatewayTimeout = 504;
    }
}
