using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BusTicket.Server.Endpoints;
using BusTicket.Server.Hubs;
using BusTicket.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Register Core Services
builder.Services.AddSingleton<DataStore>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<MomoService>();

// SignalR for Real-time WebSocket communication
builder.Services.AddSignalR();

// Enable CORS for all clients
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

app.UseCors();
app.UseWebSockets();

// Root info endpoint
app.MapGet("/", () => Results.Ok(new
{
    System = "Hệ thống Đặt vé xe khách trực tuyến (Bus Ticket Management)",
    Course = "NT106.R12 - Lập trình mạng căn bản",
    Team = "Nhóm 07 - UIT",
    Status = "Running",
    TotalApiEndpoints = 53,
    SignalRHub = "/hubs/support"
}));

// Map SignalR Hub
app.MapHub<SupportHub>("/hubs/support");

// Map all 53 REST API Endpoints
app.MapAuthEndpoints();
app.MapAccountEndpoints();
app.MapCustomerTripEndpoints();
app.MapBookingEndpoints();
app.MapNotificationEndpoints();
app.MapSupportEndpoints();
app.MapAdminEndpoints();

Console.WriteLine("=======================================================");
Console.WriteLine(" BUS TICKET SYSTEM - ASP.NET CORE WEB API & SIGNALR");
Console.WriteLine(" Môn: NT106.R12 - Nhóm 07 (UIT)");
Console.WriteLine(" Máy chủ đã khởi động với 53 REST APIs và SignalR Hub");
Console.WriteLine(" Endpoint SignalR WebSocket: /hubs/support");
Console.WriteLine("=======================================================");

app.Run();
