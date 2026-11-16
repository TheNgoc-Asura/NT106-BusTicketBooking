using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BusBooking.Server.Data;

/// <summary>
/// Hỗ trợ EF Core CLI Tools (dotnet ef migrations / database update) khởi tạo DbContext tại design-time.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BusBookingDbContext>
{
    public BusBookingDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=BusBookingDb;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<BusBookingDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new BusBookingDbContext(optionsBuilder.Options);
    }
}
