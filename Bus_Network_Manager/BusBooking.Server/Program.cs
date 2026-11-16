using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Text.Json;
using BusBooking.Server.Data;
using BusBooking.Server.Hubs;
using BusBooking.Server.Middleware;
using BusBooking.Server.Services;
using BusBooking.Shared.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Database (Entity Framework Core + MS SQL Server)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:DefaultConnection trong appsettings.json.");

builder.Services.AddDbContext<BusBookingDbContext>(options =>
    options.UseSqlServer(connectionString));

// Application Services (Auth, Email SMTP, Token Revocation, Booking & MoMo)
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ITokenRevocationService, MemoryTokenRevocationService>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<ISeatLockService, SeatLockService>();
builder.Services.AddScoped<IMomoService, MomoService>();
builder.Services.AddScoped<IPaymentConfirmationService, PaymentConfirmationService>();

// 2. Controllers & chuẩn hóa JSON camelCase + Validation error response thống nhất
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Dữ liệu không hợp lệ" : e.ErrorMessage)
            .ToList();

        string message = errors.Count > 0 ? string.Join("; ", errors) : "Dữ liệu gửi lên thiếu hoặc sai định dạng";
        return new BadRequestObjectResult(ApiResponse.Fail(message));
    };
});

// 3. JWT Bearer Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["SecretKey"] ?? throw new InvalidOperationException("Chưa cấu hình Jwt:SecretKey.");
var jwtIssuer = jwtSection["Issuer"] ?? "BusBooking.Server";
var jwtAudience = jwtSection["Audience"] ?? "BusBooking.Client";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var revocationService = context.HttpContext.RequestServices.GetRequiredService<ITokenRevocationService>();
            string? jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (!string.IsNullOrWhiteSpace(jti) && revocationService.IsTokenRevoked(jti))
            {
                context.Fail("Token đã bị thu hồi sau khi đăng xuất");
            }
            return Task.CompletedTask;
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();
            await ExceptionHandlingMiddleware.WriteErrorResponseAsync(
                context.HttpContext,
                HttpStatusCode.Unauthorized,
                "Token thiếu hoặc không hợp lệ");
        },
        OnForbidden = async context =>
        {
            await ExceptionHandlingMiddleware.WriteErrorResponseAsync(
                context.HttpContext,
                HttpStatusCode.Forbidden,
                "Không đủ quyền truy cập");
        }
    };
});

builder.Services.AddAuthorization();

// 4. SignalR (WebSocket Real-time)
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// 5. CORS
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

// 6. Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BusBooking API - NT106.R12 (Nhóm 07)",
        Version = "v1",
        Description = "ASP.NET Core Web API (.NET 8) cho hệ thống đặt vé xe trực tuyến"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT token theo định dạng: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Global exception handler (luôn trả chuẩn { success, message, data })
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(ApiResponse.Ok(new
{
    system = "Ứng dụng Đặt vé xe trực tuyến (BusBooking)",
    course = "NT106.R12 - Lập trình mạng căn bản",
    team = "Nhóm 07 - UIT",
    signalRHub = "/hubs/support",
    healthCheck = "/api/health"
}, "BusBooking Server đang hoạt động")));

app.MapControllers();
app.MapHub<SupportHub>("/hubs/support");

// Khởi tạo Database & Seed Data tự động nếu được bật
var autoInit = app.Configuration.GetValue("Database:AutoInitialize", true);
if (autoInit)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();

    try
    {
        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        await DbSeeder.SeedAsync(db);
        logger.LogInformation("Database BusBookingDb đã sẵn sàng và hoàn tất Seed Data.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Không thể tự động khởi tạo/kết nối MS SQL Server khi khởi động. Vui lòng kiểm tra ConnectionString hoặc dịch vụ SQL Server.");
    }
}

app.Run();
