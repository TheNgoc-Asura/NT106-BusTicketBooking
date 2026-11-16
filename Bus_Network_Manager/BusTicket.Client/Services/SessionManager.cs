using System;
using BusTicket.Core.DTOs;

namespace BusTicket.Client.Services;

public static class SessionManager
{
    public static string BaseUrl { get; set; } = "http://localhost:5000";
    public static string? Token { get; set; }
    public static UserInfoDto? CurrentUser { get; set; }

    public static bool IsLoggedIn => !string.IsNullOrEmpty(Token) && CurrentUser != null;

    public static void SetSession(string token, UserInfoDto user)
    {
        Token = token;
        CurrentUser = user;
    }

    public static void ClearSession()
    {
        Token = null;
        CurrentUser = null;
    }
}
