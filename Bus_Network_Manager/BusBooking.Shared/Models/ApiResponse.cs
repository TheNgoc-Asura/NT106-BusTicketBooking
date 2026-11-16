using System.Text.Json.Serialization;

namespace BusBooking.Shared.Models;

/// <summary>
/// Định dạng phản hồi chuẩn thống nhất cho toàn bộ API:
/// { "success": bool, "message": string, "data": object }
/// </summary>
public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T? data, string message = "Thành công")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Fail(string message, T? data = default)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = data
        };
    }
}

/// <summary>
/// Helper tạo phản hồi không kèm kiểu dữ liệu cụ thể (data có thể là null).
/// </summary>
public static class ApiResponse
{
    public static ApiResponse<object?> Ok(object? data = null, string message = "Thành công")
        => ApiResponse<object?>.Ok(data, message);

    public static ApiResponse<object?> Fail(string message, object? data = null)
        => ApiResponse<object?>.Fail(message, data);
}

/// <summary>
/// Cấu trúc phân trang chuẩn cho danh sách dữ liệu (Mục 5.1.4).
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;
}
