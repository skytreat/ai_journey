using System;

namespace Ipam.Frontend.Utils
{
    /// <summary>
    /// Generic API response wrapper
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public DateTime Timestamp { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow
            };
        }

        public static ApiResponse<T> Error(string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
