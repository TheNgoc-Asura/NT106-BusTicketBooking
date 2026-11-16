using BusBooking.Shared.DTOs;

namespace BusBooking.Client.Services;

public static class SessionManager
{
    public static UserInfoDto? CurrentUser { get; private set; }
    public static string? AccessToken { get; private set; }

    public static void SetSession(string token, UserInfoDto user)
    {
        AccessToken = token;
        CurrentUser = user;
        ApiClient.AccessToken = token;
    }

    public static void UpdateUserInfo(string fullName, string? phone)
    {
        if (CurrentUser == null) return;
        CurrentUser.FullName = fullName;
        CurrentUser.Phone = phone;
    }

    public static void Clear()
    {
        AccessToken = null;
        CurrentUser = null;
        ApiClient.AccessToken = null;
    }
}
