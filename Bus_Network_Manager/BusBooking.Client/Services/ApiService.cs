using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BusBooking.Shared.Models;

namespace BusBooking.Client.Services;

/// <summary>
/// Dịch vụ gọi REST API tới BusBooking.Server, tự động đính kèm Bearer JWT và parse ApiResponse chuẩn.
/// </summary>
public static class ApiService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(25)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string BaseUrl { get; set; } = "http://localhost:5000";
    public static string? AccessToken { get; set; }

    public static async Task<ApiResponse<T>> SendAsync<T>(HttpMethod method, string endpoint, object? body = null)
    {
        try
        {
            string url = $"{BaseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            using var request = new HttpRequestMessage(method, url);

            if (!string.IsNullOrWhiteSpace(AccessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            }

            if (body != null)
            {
                string json = JsonSerializer.Serialize(body, JsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            using var response = await Http.SendAsync(request);
            string raw = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(raw))
            {
                return response.IsSuccessStatusCode
                    ? ApiResponse<T>.Ok(default, "Thành công")
                    : ApiResponse<T>.Fail($"Lỗi máy chủ (HTTP {(int)response.StatusCode})");
            }

            var parsed = JsonSerializer.Deserialize<ApiResponse<T>>(raw, JsonOptions);
            if (parsed == null)
                return ApiResponse<T>.Fail("Không thể phân tích dữ liệu phản hồi từ máy chủ");

            if (!response.IsSuccessStatusCode && parsed.Success)
                parsed.Success = false;

            return parsed;
        }
        catch (TaskCanceledException)
        {
            return ApiResponse<T>.Fail("Yêu cầu quá thời gian chờ phản hồi từ Server.");
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse<T>.Fail($"Không thể kết nối tới Server ({BaseUrl}): {ex.Message}");
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.Fail($"Lỗi ứng dụng: {ex.Message}");
        }
    }

    public static Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        => SendAsync<T>(HttpMethod.Get, endpoint);

    public static Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? body = null)
        => SendAsync<T>(HttpMethod.Post, endpoint, body);

    public static Task<ApiResponse<T>> PutAsync<T>(string endpoint, object? body = null)
        => SendAsync<T>(HttpMethod.Put, endpoint, body);

    public static async Task<Image?> TryDownloadImageAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        try
        {
            using var response = await Http.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
                return null;

            string? mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType == null || !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return null;

            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            using var ms = new MemoryStream(bytes);
            using var img = Image.FromStream(ms);
            return new Bitmap(img);
        }
        catch
        {
            return null;
        }
    }

    public static async Task LogoutAsync()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(AccessToken))
            {
                await PostAsync<object>("api/auth/logout");
            }
        }
        catch
        {
            // Bỏ qua nếu server tạm ngắt khi đăng xuất
        }
    }
}
