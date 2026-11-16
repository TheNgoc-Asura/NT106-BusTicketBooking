using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusTicket.Core.DTOs;

namespace BusTicket.Client.Services;

public static class ApiClient
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    private static HttpRequestMessage CreateRequest(HttpMethod method, string endpoint, object? data = null)
    {
        string url = SessionManager.BaseUrl.TrimEnd('/') + "/" + endpoint.TrimStart('/');
        var req = new HttpRequestMessage(method, url);

        if (!string.IsNullOrEmpty(SessionManager.Token))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.Token);
        }

        if (data != null)
        {
            string json = JsonSerializer.Serialize(data);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return req;
    }

    public static async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
    {
        try
        {
            using var req = CreateRequest(HttpMethod.Get, endpoint);
            using var res = await _httpClient.SendAsync(req);
            string content = await res.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                return new ApiResponse<T>
                {
                    Success = res.IsSuccessStatusCode,
                    Message = res.IsSuccessStatusCode ? "Thành công" : $"Lỗi máy chủ (HTTP {(int)res.StatusCode})"
                };
            }

            var apiRes = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
            return apiRes ?? ApiResponse<T>.Fail("Không thể phân tích dữ liệu từ máy chủ");
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse<T>.Fail($"Không thể kết nối tới Server ({SessionManager.BaseUrl}): {ex.Message}");
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.Fail($"Lỗi: {ex.Message}");
        }
    }

    public static async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            using var req = CreateRequest(HttpMethod.Post, endpoint, data);
            using var res = await _httpClient.SendAsync(req);
            string content = await res.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                return new ApiResponse<T>
                {
                    Success = res.IsSuccessStatusCode,
                    Message = res.IsSuccessStatusCode ? "Thành công" : $"Lỗi máy chủ (HTTP {(int)res.StatusCode})"
                };
            }

            var apiRes = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
            return apiRes ?? ApiResponse<T>.Fail("Không thể phân tích dữ liệu từ máy chủ");
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse<T>.Fail($"Không thể kết nối tới Server ({SessionManager.BaseUrl}): {ex.Message}");
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.Fail($"Lỗi: {ex.Message}");
        }
    }

    public static async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            using var req = CreateRequest(HttpMethod.Put, endpoint, data);
            using var res = await _httpClient.SendAsync(req);
            string content = await res.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                return new ApiResponse<T>
                {
                    Success = res.IsSuccessStatusCode,
                    Message = res.IsSuccessStatusCode ? "Thành công" : $"Lỗi máy chủ (HTTP {(int)res.StatusCode})"
                };
            }

            var apiRes = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
            return apiRes ?? ApiResponse<T>.Fail("Không thể phân tích dữ liệu từ máy chủ");
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse<T>.Fail($"Không thể kết nối tới Server ({SessionManager.BaseUrl}): {ex.Message}");
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.Fail($"Lỗi: {ex.Message}");
        }
    }
}
