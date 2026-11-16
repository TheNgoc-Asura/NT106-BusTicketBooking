using System;
using System.Collections.Generic;

namespace BusTicket.Core.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer"; // Customer, Admin, Staff, Support
    public string Status { get; set; } = "Hoạt động"; // Hoạt động, Khóa
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class EmailVerification
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string TempToken { get; set; } = string.Empty;
    public string Type { get; set; } = "Register"; // Register, ForgotPassword
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Route
{
    public int RouteId { get; set; }
    public string RouteCode { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = "Hoạt động";
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<RouteStopPoint> StopPoints { get; set; } = new();
}

public class RouteStopPoint
{
    public int Id { get; set; }
    public int RouteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsPickup { get; set; } = true; // true = Điểm đón, false = Điểm trả
    public int OrderIndex { get; set; }
}

public class VehicleType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SeatCount { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class Vehicle
{
    public int VehicleId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public int VehicleTypeId { get; set; }
    public string Status { get; set; } = "Hoạt động"; // Hoạt động, Bảo trì, Ngừng hoạt động
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class VehicleMaintenance
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Trip
{
    public int TripId { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public int RouteId { get; set; }
    public int VehicleId { get; set; }
    public int DriverId { get; set; }
    public int? AssistantId { get; set; }
    public DateTime DepartureTime { get; set; }
    public DateTime EstimatedArrival { get; set; }
    public decimal Price { get; set; }
    public decimal Surcharge { get; set; }
    public string Status { get; set; } = "Sắp khởi hành"; // Sắp khởi hành, Đang chạy, Hoàn thành, Đã hủy
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Seat
{
    public int SeatId { get; set; }
    public int TripId { get; set; }
    public string SeatCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Trống"; // Trống, Chờ xác nhận, Đã đặt
    public decimal Price { get; set; }
    public DateTime? LockedUntil { get; set; }
}

public class Ticket
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public int TripId { get; set; }
    public int SeatId { get; set; }
    public int? CustomerId { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public string PassengerPhone { get; set; } = string.Empty;
    public string? PassengerEmail { get; set; }
    public int PickupPointId { get; set; }
    public int DropoffPointId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Channel { get; set; } = "Online"; // Online, Trực tiếp
    public string Status { get; set; } = "Chờ thanh toán"; // Chờ thanh toán, Đã thanh toán, Đã xác nhận, Đã hủy
    public DateTime BookedAt { get; set; } = DateTime.Now;
    public DateTime? PaidAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}

public class Payment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string Provider { get; set; } = "MoMo";
    public string OrderId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public long? TransId { get; set; }
    public long Amount { get; set; }
    public string PayType { get; set; } = "qr";
    public int ResultCode { get; set; } = 0;
    public string Message { get; set; } = string.Empty;
    public long? ResponseTime { get; set; }
    public string? Signature { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; } = "PaymentConfirmed";
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class SupportTicket
{
    public int TicketId { get; set; }
    public int CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Chờ xử lý"; // Chờ xử lý, Đang xử lý, Chờ phản hồi người dùng, Đã xử lý
    public int? HandlerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class SupportMessage
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int SenderId { get; set; }
    public string SenderRole { get; set; } = "Customer"; // Customer, Support, Admin
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.Now;
}
