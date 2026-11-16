using BusBooking.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Route = BusBooking.Server.Entities.Route;

namespace BusBooking.Server.Data;

public class BusBookingDbContext : DbContext
{
    public BusBookingDbContext(DbContextOptions<BusBookingDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<RoutePoint> RoutePoints => Set<RoutePoint>();
    public DbSet<VehicleType> VehicleTypes => Set<VehicleType>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<MaintenanceSchedule> MaintenanceSchedules => Set<MaintenanceSchedule>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripSeat> TripSeats => Set<TripSeat>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", tb =>
            {
                tb.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Customer', 'Admin', 'Staff', 'Support')");
                tb.HasCheckConstraint("CK_Users_Status", "[Status] IN (N'Hoạt động', N'Khóa')");
            });

            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Role);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
        });

        // 2. OtpCodes
        modelBuilder.Entity<OtpCode>(entity =>
        {
            entity.ToTable("OtpCodes", tb =>
            {
                tb.HasCheckConstraint("CK_OtpCodes_Purpose", "[Purpose] IN ('Register', 'ForgotPassword')");
            });

            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Email, e.Purpose, e.Code });
            entity.HasIndex(e => e.TempToken);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
        });

        // 3. Routes
        modelBuilder.Entity<Route>(entity =>
        {
            entity.ToTable("Routes", tb =>
            {
                tb.HasCheckConstraint("CK_Routes_Status", "[Status] IN (N'Hoạt động', N'Ngừng hoạt động')");
            });

            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RouteCode).IsUnique();
            entity.HasIndex(e => new { e.Origin, e.Destination }).IsUnique();
            entity.Property(e => e.DistanceKm).HasPrecision(8, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
        });

        // 4. RoutePoints
        modelBuilder.Entity<RoutePoint>(entity =>
        {
            entity.ToTable("RoutePoints", tb =>
            {
                tb.HasCheckConstraint("CK_RoutePoints_Type", "[Type] IN ('Pickup', 'Dropoff')");
            });

            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RouteId, e.Type });

            entity.HasOne(e => e.Route)
                .WithMany(r => r.RoutePoints)
                .HasForeignKey(e => e.RouteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 5. VehicleTypes
        modelBuilder.Entity<VehicleType>(entity =>
        {
            entity.ToTable("VehicleTypes");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // 6. Vehicles
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.ToTable("Vehicles", tb =>
            {
                tb.HasCheckConstraint("CK_Vehicles_Status", "[Status] IN (N'Hoạt động', N'Bảo trì', N'Ngừng hoạt động')");
            });

            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LicensePlate).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.VehicleType)
                .WithMany(vt => vt.Vehicles)
                .HasForeignKey(e => e.VehicleTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 7. MaintenanceSchedules
        modelBuilder.Entity<MaintenanceSchedule>(entity =>
        {
            entity.ToTable("MaintenanceSchedules");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.VehicleId);
            entity.Property(e => e.StartDate).HasColumnType("date");
            entity.Property(e => e.EndDate).HasColumnType("date");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Vehicle)
                .WithMany(v => v.MaintenanceSchedules)
                .HasForeignKey(e => e.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 8. Trips
        modelBuilder.Entity<Trip>(entity =>
        {
            entity.ToTable("Trips", tb =>
            {
                tb.HasCheckConstraint("CK_Trips_Status", "[Status] IN (N'Sắp khởi hành', N'Đang chạy', N'Hoàn thành', N'Đã hủy')");
            });

            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TripCode).IsUnique();
            entity.HasIndex(e => new { e.RouteId, e.DepartureTime });
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Surcharge).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Route)
                .WithMany(r => r.Trips)
                .HasForeignKey(e => e.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Vehicle)
                .WithMany(v => v.Trips)
                .HasForeignKey(e => e.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Driver)
                .WithMany(u => u.DrivenTrips)
                .HasForeignKey(e => e.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Assistant)
                .WithMany(u => u.AssistedTrips)
                .HasForeignKey(e => e.AssistantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 9. TripSeats
        modelBuilder.Entity<TripSeat>(entity =>
        {
            entity.ToTable("TripSeats", tb =>
            {
                tb.HasCheckConstraint("CK_TripSeats_Status", "[Status] IN (N'Trống', N'Đã đặt', N'Chờ xác nhận')");
            });

            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TripId, e.SeatCode }).IsUnique();
            entity.HasIndex(e => new { e.TripId, e.Status });

            entity.HasOne(e => e.Trip)
                .WithMany(t => t.TripSeats)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 10. Tickets
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Tickets", tb =>
            {
                tb.HasCheckConstraint("CK_Tickets_Channel", "[Channel] IN ('Online', N'Trực tiếp')");
                tb.HasCheckConstraint("CK_Tickets_Status", "[Status] IN (N'Chờ thanh toán', N'Đã thanh toán', N'Đã xác nhận', N'Đã hủy')");
            });

            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TicketCode).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.TripId);
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Trip)
                .WithMany(t => t.Tickets)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Seat)
                .WithMany(s => s.Tickets)
                .HasForeignKey(e => e.SeatId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Customer)
                .WithMany(u => u.Tickets)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PickupPoint)
                .WithMany(rp => rp.PickupTickets)
                .HasForeignKey(e => e.PickupPointId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.DropoffPoint)
                .WithMany(rp => rp.DropoffTickets)
                .HasForeignKey(e => e.DropoffPointId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 11. Notifications
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 12. SupportTickets
        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.ToTable("SupportTickets", tb =>
            {
                tb.HasCheckConstraint("CK_SupportTickets_Status", "[Status] IN (N'Chờ xử lý', N'Đang xử lý', N'Chờ phản hồi người dùng', N'Đã xử lý')");
            });

            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Customer)
                .WithMany(u => u.CustomerSupportTickets)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Handler)
                .WithMany(u => u.HandledSupportTickets)
                .HasForeignKey(e => e.HandlerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 13. SupportMessages
        modelBuilder.Entity<SupportMessage>(entity =>
        {
            entity.ToTable("SupportMessages");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SupportTicketId, e.SentAt });
            entity.Property(e => e.SentAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.SupportTicket)
                .WithMany(t => t.Messages)
                .HasForeignKey(e => e.SupportTicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Sender)
                .WithMany(u => u.SupportMessages)
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
