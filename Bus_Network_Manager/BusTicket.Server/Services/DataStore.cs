using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BusTicket.Core.Enums;
using BusTicket.Core.Models;
using Route = BusTicket.Core.Models.Route;

namespace BusTicket.Server.Services;

public class DatabaseContainer
{
    public List<User> Users { get; set; } = new();
    public List<EmailVerification> EmailVerifications { get; set; } = new();
    public List<Route> Routes { get; set; } = new();
    public List<RouteStopPoint> StopPoints { get; set; } = new();
    public List<VehicleType> VehicleTypes { get; set; } = new();
    public List<Vehicle> Vehicles { get; set; } = new();
    public List<VehicleMaintenance> Maintenances { get; set; } = new();
    public List<Trip> Trips { get; set; } = new();
    public List<Seat> Seats { get; set; } = new();
    public List<Ticket> Tickets { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
    public List<Notification> Notifications { get; set; } = new();
    public List<SupportTicket> SupportTickets { get; set; } = new();
    public List<SupportMessage> SupportMessages { get; set; } = new();
}

public class DataStore
{
    private readonly string _filePath;
    private readonly object _lock = new();
    public DatabaseContainer Db { get; private set; } = new();

    public DataStore(IWebHostEnvironment env)
    {
        string dataDir = Path.Combine(env.ContentRootPath, "data");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "bus_system_data.json");

        LoadData();
    }

    private void LoadData()
    {
        lock (_lock)
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    string json = File.ReadAllText(_filePath);
                    var data = JsonSerializer.Deserialize<DatabaseContainer>(json);
                    if (data != null && data.Users.Any())
                    {
                        Db = data;
                        return;
                    }
                }
                catch
                {
                    // If error reading, reseed
                }
            }

            SeedInitialData();
            SaveDataInternal();
        }
    }

    public void SaveChanges()
    {
        lock (_lock)
        {
            SaveDataInternal();
        }
    }

    private void SaveDataInternal()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(Db, options);
        File.WriteAllText(_filePath, json);
    }

    private void SeedInitialData()
    {
        Db = new DatabaseContainer();

        // 1. Users (Password: Matkhau@123)
        string defaultPassHash = PasswordHasher.HashPassword("Matkhau@123");
        Db.Users.AddRange(new[]
        {
            new User { Id = 1, FullName = "Quản Trị Viên Hệ Thống", Email = "admin@nhaxe.vn", PasswordHash = defaultPassHash, Phone = "0901000001", Role = Roles.Admin, Status = AccountStatuses.Active },
            new User { Id = 2, FullName = "Trần Văn B (Điều Hành)", Email = "tranvanb@nhaxe.vn", PasswordHash = defaultPassHash, Phone = "0901000002", Role = Roles.Staff, Status = AccountStatuses.Active },
            new User { Id = 3, FullName = "Lê Văn C (Tài Xế)", Email = "levanc@nhaxe.vn", PasswordHash = defaultPassHash, Phone = "0901000003", Role = Roles.Staff, Status = AccountStatuses.Active },
            new User { Id = 4, FullName = "Nguyễn Văn Tài (Phụ Xe)", Email = "nguyenvantai@nhaxe.vn", PasswordHash = defaultPassHash, Phone = "0901000004", Role = Roles.Staff, Status = AccountStatuses.Active },
            new User { Id = 5, FullName = "Phạm Thị Hỗ Trợ (CSKH)", Email = "support@nhaxe.vn", PasswordHash = defaultPassHash, Phone = "0901000005", Role = Roles.Support, Status = AccountStatuses.Active },
            new User { Id = 12, FullName = "Nguyễn Văn A (Khách Hàng)", Email = "nguyenvana@gmail.com", PasswordHash = defaultPassHash, Phone = "0901234567", Role = Roles.Customer, Status = AccountStatuses.Active },
            new User { Id = 13, FullName = "Vũ Hoàng Phúc (Khách Hàng)", Email = "phucvu@gmail.com", PasswordHash = defaultPassHash, Phone = "0987654321", Role = Roles.Customer, Status = AccountStatuses.Active }
        });

        // 2. Vehicle Types
        Db.VehicleTypes.AddRange(new[]
        {
            new VehicleType { Id = 1, Name = "Giường nằm 40 chỗ", SeatCount = 40, Description = "Xe giường nằm 2 tầng cao cấp trang bị điều hòa, wifi, cổng sạc" },
            new VehicleType { Id = 2, Name = "Limousine 9 chỗ", SeatCount = 9, Description = "Xe Limousine VIP ghế massage, màn hình giải trí riêng" },
            new VehicleType { Id = 3, Name = "Ghế ngồi 29 chỗ", SeatCount = 29, Description = "Xe ghế ngồi đời mới tiện nghi cho các tuyến ngắn" }
        });

        // 3. Vehicles
        Db.Vehicles.AddRange(new[]
        {
            new Vehicle { VehicleId = 10, LicensePlate = "51B-123.45", VehicleTypeId = 1, Status = VehicleStatuses.Active },
            new Vehicle { VehicleId = 11, LicensePlate = "51B-678.90", VehicleTypeId = 2, Status = VehicleStatuses.Active },
            new Vehicle { VehicleId = 12, LicensePlate = "51B-456.78", VehicleTypeId = 3, Status = VehicleStatuses.Active },
            new Vehicle { VehicleId = 13, LicensePlate = "51B-999.99", VehicleTypeId = 1, Status = VehicleStatuses.Maintenance }
        });

        // 4. Maintenances
        Db.Maintenances.Add(new VehicleMaintenance
        {
            Id = 5,
            VehicleId = 10,
            StartDate = DateTime.Today.AddDays(15),
            EndDate = DateTime.Today.AddDays(17),
            Note = "Bảo dưỡng định kỳ phanh và lốp xe"
        });

        // 5. Routes
        Db.Routes.AddRange(new[]
        {
            new Route { RouteId = 1, RouteCode = "T001", Origin = "TP.HCM", Destination = "Đà Lạt", DistanceKm = 308, DurationMinutes = 450, Status = RouteStatuses.Active },
            new Route { RouteId = 2, RouteCode = "T002", Origin = "TP.HCM", Destination = "Vũng Tàu", DistanceKm = 105, DurationMinutes = 150, Status = RouteStatuses.Active },
            new Route { RouteId = 3, RouteCode = "T003", Origin = "TP.HCM", Destination = "Nha Trang", DistanceKm = 435, DurationMinutes = 540, Status = RouteStatuses.Active }
        });

        // 6. Stop Points
        Db.StopPoints.AddRange(new[]
        {
            new RouteStopPoint { Id = 1, RouteId = 1, Name = "Bến xe Miền Đông Mới", Address = "501 Hoàng Hữu Nam, TP. Thủ Đức, TP.HCM", IsPickup = true, OrderIndex = 1 },
            new RouteStopPoint { Id = 2, RouteId = 1, Name = "Văn phòng Hàng Xanh", Address = "486 Điện Biên Phủ, Bình Thạnh, TP.HCM", IsPickup = true, OrderIndex = 2 },
            new RouteStopPoint { Id = 7, RouteId = 1, Name = "Bến xe Liên tỉnh Đà Lạt", Address = "01 Tô Hiến Thành, Phường 3, Đà Lạt", IsPickup = false, OrderIndex = 1 },
            new RouteStopPoint { Id = 8, RouteId = 1, Name = "Ngã 3 Đa Phú", Address = "Đường Ankroet, Phường 7, Đà Lạt", IsPickup = false, OrderIndex = 2 },
            new RouteStopPoint { Id = 3, RouteId = 2, Name = "Bến xe Miền Tây", Address = "395 Kinh Dương Vương, Bình Tân, TP.HCM", IsPickup = true, OrderIndex = 1 },
            new RouteStopPoint { Id = 4, RouteId = 2, Name = "Bến xe Vũng Tàu", Address = "Nam Kỳ Khởi Nghĩa, Vũng Tàu", IsPickup = false, OrderIndex = 1 }
        });

        // 7. Trips
        DateTime tomorrow = DateTime.Today.AddDays(1);
        Db.Trips.AddRange(new[]
        {
            new Trip
            {
                TripId = 101,
                TripCode = "CH0101",
                RouteId = 1,
                VehicleId = 10,
                DriverId = 3,
                AssistantId = 4,
                DepartureTime = tomorrow.AddHours(8),
                EstimatedArrival = tomorrow.AddHours(15).AddMinutes(30),
                Price = 280000,
                Surcharge = 0,
                Status = TripStatuses.Upcoming
            },
            new Trip
            {
                TripId = 102,
                TripCode = "CH0102",
                RouteId = 1,
                VehicleId = 10,
                DriverId = 3,
                AssistantId = 4,
                DepartureTime = tomorrow.AddHours(20),
                EstimatedArrival = tomorrow.AddDays(1).AddHours(3).AddMinutes(30),
                Price = 300000,
                Surcharge = 0,
                Status = TripStatuses.Upcoming
            },
            new Trip
            {
                TripId = 103,
                TripCode = "CH0201",
                RouteId = 2,
                VehicleId = 11,
                DriverId = 3,
                AssistantId = null,
                DepartureTime = tomorrow.AddHours(9),
                EstimatedArrival = tomorrow.AddHours(11).AddMinutes(30),
                Price = 160000,
                Surcharge = 0,
                Status = TripStatuses.Upcoming
            }
        });

        // 8. Generate Seats for Trip 101 (40 seats)
        int seatIdCounter = 5001;
        for (int i = 1; i <= 20; i++)
        {
            Db.Seats.Add(new Seat
            {
                SeatId = seatIdCounter++,
                TripId = 101,
                SeatCode = $"A{i:D2}",
                Status = i == 2 ? SeatStatuses.Booked : SeatStatuses.Available,
                Price = 280000
            });
        }
        for (int i = 1; i <= 20; i++)
        {
            Db.Seats.Add(new Seat
            {
                SeatId = seatIdCounter++,
                TripId = 101,
                SeatCode = $"B{i:D2}",
                Status = SeatStatuses.Available,
                Price = 280000
            });
        }

        // Generate Seats for Trip 103 (9 VIP seats)
        for (int i = 1; i <= 9; i++)
        {
            Db.Seats.Add(new Seat
            {
                SeatId = seatIdCounter++,
                TripId = 103,
                SeatCode = $"VIP{i}",
                Status = SeatStatuses.Available,
                Price = 160000
            });
        }

        // 9. Tickets
        Db.Tickets.Add(new Ticket
        {
            TicketId = 2001,
            TicketCode = "VE20261109001",
            TripId = 101,
            SeatId = 5002,
            CustomerId = 12,
            PassengerName = "Nguyễn Văn A",
            PassengerPhone = "0901234567",
            PassengerEmail = "nguyenvana@gmail.com",
            PickupPointId = 1,
            DropoffPointId = 7,
            TotalAmount = 280000,
            Channel = BookingChannels.Online,
            Status = TicketStatuses.Paid,
            BookedAt = DateTime.Now.AddHours(-2),
            PaidAt = DateTime.Now.AddHours(-2)
        });

        // 10. Payments
        Db.Payments.Add(new Payment
        {
            Id = 1,
            TicketId = 2001,
            Provider = "MoMo",
            OrderId = "VE20261109001",
            RequestId = "RQ1762650000001",
            TransId = 4088878653,
            Amount = 280000,
            PayType = "qr",
            ResultCode = 0,
            Message = "Successful.",
            ResponseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Signature = "7b9f4ca728076c32f16041cbc917ebf5"
        });

        // 11. Notifications
        Db.Notifications.AddRange(new[]
        {
            new Notification
            {
                Id = 301,
                UserId = 12,
                Type = "PaymentConfirmed",
                Title = "Thanh toán thành công",
                Content = "Vé VE20261109001 đã được thanh toán thành công.",
                IsRead = false,
                CreatedAt = DateTime.Now.AddHours(-2)
            },
            new Notification
            {
                Id = 302,
                UserId = 12,
                Type = "TripReminder",
                Title = "Nhắc nhở giờ khởi hành",
                Content = "Chuyến xe CH0101 sẽ khởi hành lúc 08:00 ngày mai tại Bến xe Miền Đông.",
                IsRead = false,
                CreatedAt = DateTime.Now.AddHours(-1)
            }
        });

        // 12. Support Tickets & Messages
        Db.SupportTickets.Add(new SupportTicket
        {
            TicketId = 77,
            CustomerId = 12,
            Title = "Hỏi về ghế chuyến 8h",
            Status = SupportTicketStatuses.Processing,
            HandlerId = 5,
            CreatedAt = DateTime.Now.AddHours(-1),
            UpdatedAt = DateTime.Now.AddMinutes(-30)
        });

        Db.SupportMessages.AddRange(new[]
        {
            new SupportMessage
            {
                Id = 9001,
                TicketId = 77,
                SenderId = 12,
                SenderRole = Roles.Customer,
                Content = "Cho mình hỏi chuyến 8h còn ghế không?",
                SentAt = DateTime.Now.AddMinutes(-35)
            },
            new SupportMessage
            {
                Id = 9002,
                TicketId = 77,
                SenderId = 5,
                SenderRole = Roles.Support,
                Content = "Chào bạn, chuyến 8h còn ghế trống ạ.",
                SentAt = DateTime.Now.AddMinutes(-30)
            }
        });
    }

    public int GetNextUserId() => Db.Users.Any() ? Db.Users.Max(u => u.Id) + 1 : 1;
    public int GetNextRouteId() => Db.Routes.Any() ? Db.Routes.Max(r => r.RouteId) + 1 : 1;
    public int GetNextVehicleId() => Db.Vehicles.Any() ? Db.Vehicles.Max(v => v.VehicleId) + 1 : 1;
    public int GetNextTripId() => Db.Trips.Any() ? Db.Trips.Max(t => t.TripId) + 1 : 1;
    public int GetNextSeatId() => Db.Seats.Any() ? Db.Seats.Max(s => s.SeatId) + 1 : 1;
    public int GetNextTicketId() => Db.Tickets.Any() ? Db.Tickets.Max(t => t.TicketId) + 1 : 1;
    public int GetNextNotificationId() => Db.Notifications.Any() ? Db.Notifications.Max(n => n.Id) + 1 : 1;
    public int GetNextSupportTicketId() => Db.SupportTickets.Any() ? Db.SupportTickets.Max(s => s.TicketId) + 1 : 1;
    public int GetNextSupportMessageId() => Db.SupportMessages.Any() ? Db.SupportMessages.Max(m => m.Id) + 1 : 1;
    public int GetNextMaintenanceId() => Db.Maintenances.Any() ? Db.Maintenances.Max(m => m.Id) + 1 : 1;
    public int GetNextStopPointId() => Db.StopPoints.Any() ? Db.StopPoints.Max(p => p.Id) + 1 : 1;

    public string GenerateTripCode() => $"CH{GetNextTripId():D4}";
    public string GenerateRouteCode() => $"T{GetNextRouteId():D3}";
    public string GenerateTicketCode() => $"VE{DateTime.Now:yyyyMMdd}{GetNextTicketId():D4}";
}
