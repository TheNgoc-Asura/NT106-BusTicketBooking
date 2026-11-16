using System;
using System.Collections.Generic;

namespace BusTicket.Core.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = "Thành công";
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T? data, string message = "Thành công")
    {
        return new ApiResponse<T> { Success = true, Message = message, Data = data };
    }

    public static ApiResponse<T> Fail(string message)
    {
        return new ApiResponse<T> { Success = false, Message = message, Data = default };
    }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;
}

// ---------------- AUTH DTOs ----------------
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseData
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserInfoDto User { get; set; } = new();
}

public class UserInfoDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class RegisterRequestCodeDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RequestCodeResponseData
{
    public string Email { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; } = 300;
}

public class VerifyCodeDto
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class VerifyCodeResponseData
{
    public string TempToken { get; set; } = string.Empty;
}

public class CompleteRegisterDto
{
    public string TempToken { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CompleteRegisterResponseData
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class ForgotPasswordRequestCodeDto
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    public string TempToken { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ProfileDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UpdateProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class ChangePasswordDto
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

// ---------------- TRIPS & SEATS DTOs ----------------
public class TripDto
{
    public int TripId { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime EstimatedArrival { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
}

public class TripSeatsResponseData
{
    public int TripId { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public List<SeatDto> Seats { get; set; } = new();
    public List<StopPointDto> PickupPoints { get; set; } = new();
    public List<StopPointDto> DropoffPoints { get; set; } = new();
}

public class SeatDto
{
    public int SeatId { get; set; }
    public string SeatCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Trống";
    public decimal Price { get; set; }
}

public class StopPointDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
}

// ---------------- BOOKINGS & PAYMENT DTOs ----------------
public class CreateBookingRequest
{
    public int TripId { get; set; }
    public int SeatId { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public string PassengerPhone { get; set; } = string.Empty;
    public string? PassengerEmail { get; set; }
    public int PickupPointId { get; set; }
    public int DropoffPointId { get; set; }
}

public class BookingResponseData
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Chờ thanh toán";
    public decimal TotalAmount { get; set; }
    public PaymentInfoData Payment { get; set; } = new();
}

public class PaymentInfoData
{
    public string Provider { get; set; } = "MoMo";
    public string OrderId { get; set; } = string.Empty;
    public string PayUrl { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
}

public class MomoIpnRequest
{
    public string PartnerCode { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string OrderInfo { get; set; } = string.Empty;
    public string OrderType { get; set; } = "momo_wallet";
    public long TransId { get; set; }
    public int ResultCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PayType { get; set; } = "qr";
    public long ResponseTime { get; set; }
    public string ExtraData { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

public class PaymentStatusResponseData
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
}

// ---------------- TICKETS DTOs ----------------
public class MyTicketDto
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public string SeatCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TicketDetailDto
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public TripSummaryDto Trip { get; set; } = new();
    public string SeatCode { get; set; } = string.Empty;
    public PassengerDto Passenger { get; set; } = new();
    public string PickupPoint { get; set; } = string.Empty;
    public string DropoffPoint { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = "MoMo";
    public DateTime? PaidAt { get; set; }
}

public class TripSummaryDto
{
    public int TripId { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
}

public class PassengerDto
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

// ---------------- NOTIFICATIONS DTOs ----------------
public class NotificationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = "PaymentConfirmed";
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ---------------- SUPPORT DTOs ----------------
public class SupportMessageDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int SenderId { get; set; }
    public string SenderRole { get; set; } = "Customer";
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}

public class CreateSupportMessageDto
{
    public string Content { get; set; } = string.Empty;
}

public class SupportTicketSummaryDto
{
    public int TicketId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = "Chờ xử lý";
    public string? HandlerName { get; set; }
    public DateTime LastMessageAt { get; set; }
}

public class SupportStatisticsDto
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Processing { get; set; }
    public int WaitingCustomer { get; set; }
    public int Resolved { get; set; }
}

public class AdminSupportTicketListResponseData
{
    public SupportStatisticsDto Statistics { get; set; } = new();
    public PagedResult<SupportTicketSummaryDto> Tickets { get; set; } = new();
}

public class UpdateTicketStatusRequest
{
    public string Status { get; set; } = "Đang xử lý"; // Đang xử lý, Đã xử lý
}

// ---------------- ADMIN DASHBOARD DTOs ----------------
public class DashboardStatsDto
{
    public int TicketsToday { get; set; }
    public int TripsToday { get; set; }
    public decimal RevenueToday { get; set; }
    public List<UpcomingTripDto> UpcomingTrips { get; set; } = new();
}

public class UpcomingTripDto
{
    public int TripId { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public int BookedSeats { get; set; }
    public int TotalSeats { get; set; }
}

// ---------------- ADMIN TRIPS DTOs ----------------
public class AdminTripDto
{
    public int TripId { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string AssistantName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int BookedSeats { get; set; }
    public int TotalSeats { get; set; }
    public string Status { get; set; } = "Sắp khởi hành";
}

public class TripFormOptionsDto
{
    public List<OptionItemDto> Routes { get; set; } = new();
    public List<VehicleOptionDto> Vehicles { get; set; } = new();
    public List<OptionItemDto> Drivers { get; set; } = new();
    public List<OptionItemDto> Assistants { get; set; } = new();
}

public class OptionItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get => Name; set => Name = value; }
}

public class VehicleOptionDto
{
    public int Id { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
}

public class CreateTripRequest
{
    public int RouteId { get; set; }
    public DateTime DepartureTime { get; set; }
    public int VehicleId { get; set; }
    public int DriverId { get; set; }
    public int? AssistantId { get; set; }
    public decimal Price { get; set; }
    public decimal Surcharge { get; set; }
}

public class UpdateTripRequest : CreateTripRequest
{
    public string Status { get; set; } = "Sắp khởi hành";
}

// ---------------- ADMIN ROUTES DTOs ----------------
public class AdminRouteDto
{
    public int RouteId { get; set; }
    public string RouteCode { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Status { get; set; } = "Hoạt động";
    public int UpcomingTrips { get; set; }
}

public class RouteDetailDto
{
    public int RouteId { get; set; }
    public string RouteCode { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = "Hoạt động";
    public List<StopPointDto> PickupPoints { get; set; } = new();
    public List<StopPointDto> DropoffPoints { get; set; } = new();
}

public class CreateRouteRequest
{
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public int? DurationMinutes { get; set; }
    public List<StopPointInputDto> PickupPoints { get; set; } = new();
    public List<StopPointInputDto> DropoffPoints { get; set; } = new();
}

public class UpdateRouteRequest : CreateRouteRequest
{
    public string Status { get; set; } = "Hoạt động";
}

public class StopPointInputDto
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

// ---------------- ADMIN VEHICLES & MAINTENANCE DTOs ----------------
public class AdminVehicleDto
{
    public int VehicleId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public int VehicleTypeId { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public string Status { get; set; } = "Hoạt động";
}

public class VehicleTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SeatCount { get; set; }
}

public class CreateVehicleRequest
{
    public string LicensePlate { get; set; } = string.Empty;
    public int VehicleTypeId { get; set; }
}

public class MaintenanceScheduleDto
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public class MaintenanceResponseData
{
    public List<AdminVehicleDto> Vehicles { get; set; } = new();
    public List<MaintenanceScheduleDto> Schedules { get; set; } = new();
}

public class CreateMaintenanceRequest
{
    public int VehicleId { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string? Note { get; set; }
}

// ---------------- ADMIN TICKETS DTOs ----------------
public class AdminTicketDto
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string TripCode { get; set; } = string.Empty;
    public string PassengerName { get; set; } = string.Empty;
    public string SeatCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Channel { get; set; } = "Online";
    public string Status { get; set; } = "Đã thanh toán";
}

public class CounterBookingRequest
{
    public int TripId { get; set; }
    public int SeatId { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public string PassengerPhone { get; set; } = string.Empty;
    public string? PassengerEmail { get; set; }
    public int PickupPointId { get; set; }
    public int DropoffPointId { get; set; }
}

// ---------------- ADMIN STAFF & PERMISSIONS DTOs ----------------
public class StaffAccountDto
{
    public int AccountId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = "Hoạt động";
}

public class RolePermissionDto
{
    public string Role { get; set; } = string.Empty;
    public List<string> Functions { get; set; } = new();
}

public class StaffAccountsResponseData
{
    public List<StaffAccountDto> Accounts { get; set; } = new();
    public List<RolePermissionDto> Permissions { get; set; } = new();
}

// ---------------- ADMIN CUSTOMERS DTOs ----------------
public class CustomerItemDto
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TicketCount { get; set; }
}

public class CustomerDetailData
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public List<MyTicketDto> Tickets { get; set; } = new();
}

// ---------------- ADMIN REVENUE DTOs ----------------
public class RevenueReportDto
{
    public string FromDate { get; set; } = string.Empty;
    public string ToDate { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public decimal OnlineRevenue { get; set; }
    public decimal OfflineRevenue { get; set; }
    public List<DailyRevenueDto> Details { get; set; } = new();
}

public class DailyRevenueDto
{
    public string Date { get; set; } = string.Empty;
    public decimal Online { get; set; }
    public decimal Offline { get; set; }
    public decimal Total { get; set; }
}
