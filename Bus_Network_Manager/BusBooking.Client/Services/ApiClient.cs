using BusBooking.Shared.Models;

namespace BusBooking.Client.Services;

/// <summary>
/// Lớp tương thích chuyển tiếp sang ApiService.
/// </summary>
public static class ApiClient
{
    public static string BaseUrl
    {
        get => ApiService.BaseUrl;
        set => ApiService.BaseUrl = value;
    }

    public static string? AccessToken
    {
        get => ApiService.AccessToken;
        set => ApiService.AccessToken = value;
    }

    public static Task<ApiResponse<T>> GetAsync<T>(string endpoint) => ApiService.GetAsync<T>(endpoint);
    public static Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? body = null) => ApiService.PostAsync<T>(endpoint, body);
    public static Task<ApiResponse<T>> PutAsync<T>(string endpoint, object? body = null) => ApiService.PutAsync<T>(endpoint, body);
    public static Task LogoutAsync() => ApiService.LogoutAsync();
}
