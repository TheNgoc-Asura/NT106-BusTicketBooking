using BusBooking.Server.Entities;
using BusBooking.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Route = BusBooking.Server.Entities.Route;

namespace BusBooking.Server.Data;

public static class DbSeeder
{
    /// <summary>
    /// Khởi tạo dữ liệu mẫu idempotent khi Database mới được tạo hoặc chưa có người dùng.
    /// Mật khẩu mặc định của tất cả tài khoản mẫu: Matkhau@123
    /// </summary>
    public static async Task SeedAsync(BusBookingDbContext db)
    {
        if (await db.Users.AnyAsync())
            return;

        var now = DateTime.Now;
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Matkhau@123");

        // 1. Tài khoản mẫu cho 4 vai trò (Admin, Staff, Support, Customer)
        var admin = new User
        {
            FullName = "Quản Trị Viên Hệ Thống",
            Email = "admin@nhaxe.vn",
            Phone = "0901000001",
            PasswordHash = passwordHash,
            Role = UserRoles.Admin,
            Status = AccountStatuses.Active,
            CreatedAt = now
        };

        var staff = new User
        {
            FullName = "Trần Văn B",
            Email = "staff@nhaxe.vn",
            Phone = "0901000002",
            PasswordHash = passwordHash,
            Role = UserRoles.Staff,
            Status = AccountStatuses.Active,
            CreatedAt = now
        };

        var support = new User
        {
            FullName = "Phạm Thị Hỗ Trợ",
            Email = "support@nhaxe.vn",
            Phone = "0901000003",
            PasswordHash = passwordHash,
            Role = UserRoles.Support,
            Status = AccountStatuses.Active,
            CreatedAt = now
        };

        var customer = new User
        {
            FullName = "Nguyễn Văn A",
            Email = "nguyenvana@gmail.com",
            Phone = "0901234567",
            PasswordHash = passwordHash,
            Role = UserRoles.Customer,
            Status = AccountStatuses.Active,
            CreatedAt = now
        };

        db.Users.AddRange(admin, staff, support, customer);
        await db.SaveChangesAsync();

        // 2. Loại xe (40 chỗ & Limousine 9 chỗ)
        var sleeper40 = new VehicleType
        {
            Name = "Giường nằm 40 chỗ",
            SeatCount = 40,
            Description = "Xe giường nằm 2 tầng cao cấp, điều hòa, wifi, cổng sạc USB"
        };

        var limousine9 = new VehicleType
        {
            Name = "Limousine 9 chỗ",
            SeatCount = 9,
            Description = "Xe Limousine VIP 9 ghế massage hạng thương gia"
        };

        db.VehicleTypes.AddRange(sleeper40, limousine9);
        await db.SaveChangesAsync();

        // 3. Đội xe & Lịch bảo trì
        var bus40A = new Vehicle
        {
            LicensePlate = "51B-123.45",
            VehicleTypeId = sleeper40.Id,
            Status = VehicleStatuses.Active,
            CreatedAt = now
        };

        var bus40B = new Vehicle
        {
            LicensePlate = "51B-246.80",
            VehicleTypeId = sleeper40.Id,
            Status = VehicleStatuses.Active,
            CreatedAt = now
        };

        var busLimo = new Vehicle
        {
            LicensePlate = "51B-678.90",
            VehicleTypeId = limousine9.Id,
            Status = VehicleStatuses.Active,
            CreatedAt = now
        };

        var busMaint = new Vehicle
        {
            LicensePlate = "51B-999.99",
            VehicleTypeId = sleeper40.Id,
            Status = VehicleStatuses.Maintenance,
            CreatedAt = now
        };

        db.Vehicles.AddRange(bus40A, bus40B, busLimo, busMaint);
        await db.SaveChangesAsync();

        db.MaintenanceSchedules.Add(new MaintenanceSchedule
        {
            VehicleId = busMaint.Id,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(3),
            Note = "Bảo dưỡng định kỳ phanh, lốp và hệ thống điều hòa",
            CreatedAt = now
        });

        // 4. Tuyến xe (TP.HCM - Đà Lạt, TP.HCM - Vũng Tàu, Đà Lạt - TP.HCM)
        var routeDalat = new Route
        {
            RouteCode = "T001",
            Origin = "TP.HCM",
            Destination = "Đà Lạt",
            DistanceKm = 308,
            DurationMinutes = 450,
            Status = RouteStatuses.Active,
            CreatedAt = now
        };

        var routeVungTau = new Route
        {
            RouteCode = "T002",
            Origin = "TP.HCM",
            Destination = "Vũng Tàu",
            DistanceKm = 105,
            DurationMinutes = 150,
            Status = RouteStatuses.Active,
            CreatedAt = now
        };

        var routeReturn = new Route
        {
            RouteCode = "T003",
            Origin = "Đà Lạt",
            Destination = "TP.HCM",
            DistanceKm = 308,
            DurationMinutes = 450,
            Status = RouteStatuses.Active,
            CreatedAt = now
        };

        db.Routes.AddRange(routeDalat, routeVungTau, routeReturn);
        await db.SaveChangesAsync();

        // 5. Điểm đón / Điểm trả của từng tuyến
        var dlPickup1 = new RoutePoint { RouteId = routeDalat.Id, Type = RoutePointTypes.Pickup, Name = "Bến xe Miền Đông Mới", Address = "501 Hoàng Hữu Nam, TP. Thủ Đức, TP.HCM", OrderIndex = 1 };
        var dlPickup2 = new RoutePoint { RouteId = routeDalat.Id, Type = RoutePointTypes.Pickup, Name = "Văn phòng Hàng Xanh", Address = "486 Điện Biên Phủ, Bình Thạnh, TP.HCM", OrderIndex = 2 };
        var dlDropoff1 = new RoutePoint { RouteId = routeDalat.Id, Type = RoutePointTypes.Dropoff, Name = "Bến xe Liên tỉnh Đà Lạt", Address = "01 Tô Hiến Thành, Phường 3, Đà Lạt", OrderIndex = 1 };
        var dlDropoff2 = new RoutePoint { RouteId = routeDalat.Id, Type = RoutePointTypes.Dropoff, Name = "Ngã 3 Đa Phú", Address = "Đường Ankroet, Phường 7, Đà Lạt", OrderIndex = 2 };

        var vtPickup1 = new RoutePoint { RouteId = routeVungTau.Id, Type = RoutePointTypes.Pickup, Name = "Bến xe Miền Tây", Address = "395 Kinh Dương Vương, Bình Tân, TP.HCM", OrderIndex = 1 };
        var vtPickup2 = new RoutePoint { RouteId = routeVungTau.Id, Type = RoutePointTypes.Pickup, Name = "Trạm Hàng Xanh", Address = "486 Điện Biên Phủ, Bình Thạnh, TP.HCM", OrderIndex = 2 };
        var vtDropoff1 = new RoutePoint { RouteId = routeVungTau.Id, Type = RoutePointTypes.Dropoff, Name = "Bến xe Vũng Tàu", Address = "192 Nam Kỳ Khởi Nghĩa, TP. Vũng Tàu", OrderIndex = 1 };

        var rtPickup1 = new RoutePoint { RouteId = routeReturn.Id, Type = RoutePointTypes.Pickup, Name = "Bến xe Liên tỉnh Đà Lạt", Address = "01 Tô Hiến Thành, Phường 3, Đà Lạt", OrderIndex = 1 };
        var rtDropoff1 = new RoutePoint { RouteId = routeReturn.Id, Type = RoutePointTypes.Dropoff, Name = "Bến xe Miền Đông Mới", Address = "501 Hoàng Hữu Nam, TP. Thủ Đức, TP.HCM", OrderIndex = 1 };

        db.RoutePoints.AddRange(
            dlPickup1, dlPickup2, dlDropoff1, dlDropoff2,
            vtPickup1, vtPickup2, vtDropoff1,
            rtPickup1, rtDropoff1
        );
        await db.SaveChangesAsync();

        // 6. Các chuyến xe sắp khởi hành (ngày mai & ngày kia)
        var tomorrow = DateTime.Today.AddDays(1);
        var dayAfter = DateTime.Today.AddDays(2);

        var trip1 = new Trip
        {
            TripCode = "CH0101",
            RouteId = routeDalat.Id,
            VehicleId = bus40A.Id,
            DriverId = staff.Id,
            AssistantId = null,
            DepartureTime = tomorrow.AddHours(8),
            EstimatedArrival = tomorrow.AddHours(15).AddMinutes(30),
            Price = 280000,
            Surcharge = 0,
            Status = TripStatuses.Upcoming,
            CreatedAt = now
        };

        var trip2 = new Trip
        {
            TripCode = "CH0102",
            RouteId = routeDalat.Id,
            VehicleId = bus40B.Id,
            DriverId = staff.Id,
            AssistantId = null,
            DepartureTime = tomorrow.AddHours(20),
            EstimatedArrival = dayAfter.AddHours(3).AddMinutes(30),
            Price = 300000,
            Surcharge = 0,
            Status = TripStatuses.Upcoming,
            CreatedAt = now
        };

        var trip3 = new Trip
        {
            TripCode = "CH0201",
            RouteId = routeVungTau.Id,
            VehicleId = busLimo.Id,
            DriverId = staff.Id,
            AssistantId = null,
            DepartureTime = tomorrow.AddHours(9),
            EstimatedArrival = tomorrow.AddHours(11).AddMinutes(30),
            Price = 180000,
            Surcharge = 0,
            Status = TripStatuses.Upcoming,
            CreatedAt = now
        };

        var trip4 = new Trip
        {
            TripCode = "CH0301",
            RouteId = routeReturn.Id,
            VehicleId = bus40A.Id,
            DriverId = staff.Id,
            AssistantId = null,
            DepartureTime = dayAfter.AddHours(10),
            EstimatedArrival = dayAfter.AddHours(17).AddMinutes(30),
            Price = 280000,
            Surcharge = 0,
            Status = TripStatuses.Upcoming,
            CreatedAt = now
        };

        db.Trips.AddRange(trip1, trip2, trip3, trip4);
        await db.SaveChangesAsync();

        // 7. Sinh đầy đủ ghế cho cả 4 chuyến
        var seatsTrip1 = GenerateSeatsForTrip(trip1.Id, sleeper40.SeatCount);
        var seatsTrip2 = GenerateSeatsForTrip(trip2.Id, sleeper40.SeatCount);
        var seatsTrip3 = GenerateSeatsForTrip(trip3.Id, limousine9.SeatCount);
        var seatsTrip4 = GenerateSeatsForTrip(trip4.Id, sleeper40.SeatCount);

        // Đánh dấu 1 ghế mẫu đã đặt trên chuyến CH0101 (A01)
        seatsTrip1[0].Status = SeatStatuses.Booked;

        db.TripSeats.AddRange(seatsTrip1);
        db.TripSeats.AddRange(seatsTrip2);
        db.TripSeats.AddRange(seatsTrip3);
        db.TripSeats.AddRange(seatsTrip4);
        await db.SaveChangesAsync();

        // 8. Vé mẫu đã thanh toán cho Customer
        var sampleTicket = new Ticket
        {
            TicketCode = $"VE{now:yyyyMMdd}0001",
            TripId = trip1.Id,
            SeatId = seatsTrip1[0].Id,
            CustomerId = customer.Id,
            PassengerName = customer.FullName,
            PassengerPhone = customer.Phone ?? "0901234567",
            PassengerEmail = customer.Email,
            PickupPointId = dlPickup1.Id,
            DropoffPointId = dlDropoff1.Id,
            TotalAmount = trip1.Price + trip1.Surcharge,
            Channel = BookingChannels.Online,
            Status = TicketStatuses.Paid,
            PaymentMethod = PaymentMethods.MoMo,
            PaymentTransId = 4088878653,
            PaidAt = now.AddHours(-1),
            CreatedAt = now.AddHours(-1)
        };
        db.Tickets.Add(sampleTicket);

        // 9. Thông báo mẫu
        db.Notifications.AddRange(
            new Notification
            {
                UserId = customer.Id,
                Type = NotificationTypes.PaymentConfirmed,
                Title = "Thanh toán thành công",
                Content = $"Vé {sampleTicket.TicketCode} tuyến TP.HCM - Đà Lạt đã được thanh toán qua MoMo.",
                IsRead = false,
                CreatedAt = now.AddHours(-1)
            },
            new Notification
            {
                UserId = customer.Id,
                Type = NotificationTypes.TripReminder,
                Title = "Nhắc nhở giờ khởi hành",
                Content = "Chuyến xe CH0101 sẽ khởi hành lúc 08:00 ngày mai tại Bến xe Miền Đông Mới.",
                IsRead = false,
                CreatedAt = now.AddMinutes(-30)
            }
        );

        // 10. Ticket hỗ trợ & tin nhắn mẫu
        var supportTicket = new SupportTicket
        {
            CustomerId = customer.Id,
            Title = "Hỏi về điểm đón chuyến TP.HCM - Đà Lạt 08:00",
            Status = SupportTicketStatuses.Processing,
            HandlerId = support.Id,
            CreatedAt = now.AddMinutes(-25),
            UpdatedAt = now.AddMinutes(-15)
        };
        db.SupportTickets.Add(supportTicket);
        await db.SaveChangesAsync();

        db.SupportMessages.AddRange(
            new SupportMessage
            {
                SupportTicketId = supportTicket.Id,
                SenderId = customer.Id,
                SenderRole = UserRoles.Customer,
                Content = "Chào nhà xe, cho mình hỏi chuyến 8h sáng mai có đón ở Hàng Xanh không ạ?",
                SentAt = now.AddMinutes(-25)
            },
            new SupportMessage
            {
                SupportTicketId = supportTicket.Id,
                SenderId = support.Id,
                SenderRole = UserRoles.Support,
                Content = "Chào bạn, chuyến CH0101 có điểm đón tại Văn phòng Hàng Xanh (486 Điện Biên Phủ) ạ.",
                SentAt = now.AddMinutes(-15)
            }
        );

        await db.SaveChangesAsync();
    }

    public static List<TripSeat> GenerateSeatsForTrip(int tripId, int seatCount)
    {
        var seats = new List<TripSeat>(seatCount);
        if (seatCount <= 9)
        {
            for (int i = 1; i <= seatCount; i++)
            {
                seats.Add(new TripSeat
                {
                    TripId = tripId,
                    SeatCode = $"VIP{i:D2}",
                    Status = SeatStatuses.Available
                });
            }
        }
        else
        {
            int half = seatCount / 2;
            for (int i = 1; i <= half; i++)
            {
                seats.Add(new TripSeat
                {
                    TripId = tripId,
                    SeatCode = $"A{i:D2}",
                    Status = SeatStatuses.Available
                });
            }
            for (int i = 1; i <= seatCount - half; i++)
            {
                seats.Add(new TripSeat
                {
                    TripId = tripId,
                    SeatCode = $"B{i:D2}",
                    Status = SeatStatuses.Available
                });
            }
        }

        return seats;
    }
}
